using System;
using System.Collections.Generic;
using System.Reflection;

namespace Kyub.Serialization.Internal {
    public static class VersionManager {
        private static readonly Dictionary<Type, Option<VersionedType>> _cache = new Dictionary<Type, Option<VersionedType>>();

        public static Result GetVersionImportPath(string currentVersion, VersionedType targetVersion, out List<VersionedType> path) {
            path = new List<VersionedType>();

            if (GetVersionImportPathRecursive(path, currentVersion, targetVersion) == false) {
                return Result.Fail("There is no migration path from \"" + currentVersion + "\" to \"" + targetVersion.VersionString + "\"");
            }

            path.Add(targetVersion);
            return Result.Success;
        }

        private static bool GetVersionImportPathRecursive(List<VersionedType> path, string currentVersion, VersionedType current) {
            for (int i = 0; i < current.Ancestors.Length; ++i) {
                VersionedType ancestor = current.Ancestors[i];

                if (ancestor.VersionString == currentVersion ||
                    GetVersionImportPathRecursive(path, currentVersion, ancestor)) {

                    path.Add(ancestor);
                    return true;
                }
            }

            return false;
        }

        public static Option<VersionedType> GetVersionedType(Type type) {
            Option<VersionedType> optionalVersionedType;

            if (_cache.TryGetValue(type, out optionalVersionedType) == false) {
                var attr = PortableReflection.GetAttribute<ObjectAttribute>(type);

                if (attr != null) {
                    if (string.IsNullOrEmpty(attr.VersionString) == false || attr.PreviousModels != null) {
                        // Version string must be provided
                        if (attr.PreviousModels != null && string.IsNullOrEmpty(attr.VersionString)) {
                            throw new Exception("Object attribute on " + type + " contains a PreviousModels specifier - it must also include a VersionString modifier");
                        }

                        // Map the ancestor types into versioned types
                        VersionedType[] ancestors = new VersionedType[attr.PreviousModels != null ? attr.PreviousModels.Length : 0];
                        for (int i = 0; i < ancestors.Length; ++i) {
                            Option<VersionedType> ancestorType = GetVersionedType(attr.PreviousModels[i]);
                            if (ancestorType.IsEmpty) {
                                throw new Exception("Unable to create versioned type for ancestor " + ancestorType + "; please add an [Object(VersionString=\"...\")] attribute");
                            }
                            ancestors[i] = ancestorType.Value;
                        }

                        // construct the actual versioned type instance
                        VersionedType versionedType = new VersionedType {
                            Ancestors = ancestors,
                            VersionString = attr.VersionString,
                            ModelType = type
                        };

                        // finally, verify that the versioned type passes some sanity checks
                        VerifyUniqueVersionStrings(versionedType);
                        VerifyConstructors(versionedType);

                        optionalVersionedType = Option.Just(versionedType);
                    }
                }

                _cache[type] = optionalVersionedType;
            }

            return optionalVersionedType;
        }

        /// <summary>
        /// Verifies that the given type has constructors to migrate from all ancestor types.
        /// </summary>
        private static void VerifyConstructors(VersionedType type) {
            ConstructorInfo[] publicConstructors = type.ModelType.GetDeclaredConstructors();

            for (int i = 0; i < type.Ancestors.Length; ++i) {
                Type requiredConstructorType = type.Ancestors[i].ModelType;

                bool found = false;
                for (int j = 0; j < publicConstructors.Length; ++j) {
                    var parameters = publicConstructors[j].GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType == requiredConstructorType) {
                        found = true;
                        break;
                    }
                }

                if (found == false) {
                    throw new MissingVersionConstructorException(type.ModelType, requiredConstructorType);
                }
            }
        }

        /// <summary>
        /// Verifies that the given version graph contains only unique versions.
        /// </summary>
        private static void VerifyUniqueVersionStrings(VersionedType type) {
            // simple tree traversal

            var found = new Dictionary<string, Type>();

            var remaining = new Queue<VersionedType>();
            remaining.Enqueue(type);

            while (remaining.Count > 0) {
                VersionedType item = remaining.Dequeue();

                // Verify we do not already have the version string. Take into account that we're not just
                // comparing the same model twice, since we can have a valid import graph that has the same
                // model multiple times.
                if (found.ContainsKey(item.VersionString) && found[item.VersionString] != item.ModelType) {
                    throw new DuplicateVersionNameException(found[item.VersionString], item.ModelType, item.VersionString);
                }
                found[item.VersionString] = item.ModelType;

                // scan the ancestors as well
                foreach (var ancestor in item.Ancestors) {
                    remaining.Enqueue(ancestor);
                }
            }
        }
    }
}