// Based in MaterialUI originally found in https://github.com/InvexGames/MaterialUI
// Kyub Interactive LTDA 2022. 

namespace MaterialUI
{
    /// <summary>
    /// Implemented by an object that wants to be able to create and manage ripples.
    /// </summary>
    public interface IRippleCreator
    {
        /// <summary>
        /// Called when a ripple is created.
        /// </summary>
        void OnCreateRipple();
        /// <summary>
        /// Called when a ripple is destroyed.
        /// </summary>
        void OnDestroyRipple();

        /// <summary>
        /// Gets the ripple data.
        /// </summary>
        /// <value>
        /// The ripple data.
        /// </value>
        RippleData rippleData { get; }
    }

    /// <summary>
    /// Allows an object to handle text validation.
    /// </summary>
    public interface ITextValidator
    {
        /// <summary>
        /// Initializes the specified material input field.
        /// </summary>
        /// <param name="materialInputField">The material input field.</param>
        void Init(MaterialInputField materialInputField);

        /// <summary>
        /// Dispose Validator
        /// </summary>
        void Dispose();

        /// <summary>
        /// Determines whether the text is valid.
        /// </summary>
        /// <returns></returns>
        bool IsTextValid();

        ITextValidator Clone();
    }

    public interface IAutoFormatTextValidator : ITextValidator
    {
        /// <summary>
        /// Format Text into a specific validator format
        /// </summary>
        bool FormatText();
    }

    /// <summary>
    /// Container
    /// </summary>
    public interface IOptionDataListContainer
    {
        /// <summary>
        /// Gets or sets the option data list.
        /// </summary>
        /// <value>
        /// The option data list.
        /// </value>
        OptionDataList optionDataList { get; set; }
    }
}