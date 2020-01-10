* Copy upm-push.cmd to your package root folder (require package.json file).
* Commit to Master First
* Double-click in 'upm-push.cmd' and wait
* Wait until the script generate a new branch <PACKAGE_NAME> with tag equal to <PACKAGE_NAME>-<VERSION>
* Create new Personal Acess Token with 'read_repository' access
(https://docs.gitlab.com/ee/user/profile/personal_access_tokens.html)

Now you a ready to add it as dependence in Packages/manifest.json (in your current project)

the pattern of the Gitlab clone repository is:
https://gitlab-ci-token:<PersonalAccessToken>@gitlab.com/<CLONE_REPOSITORY_PATH>.git#<PACKAGE_NAME>-<VERSION>

Example:
package 'com.kyub.core'can be accessed using link below (Head Version)
https://gitlab-ci-token:zF563oCqvXdJmiDuMShq@gitlab.com/KyubInteractive/kyublibs.git#com.kyub.core

OR using specific version

https://gitlab-ci-token:zF563oCqvXdJmiDuMShq@gitlab.com/KyubInteractive/kyublibs.git#com.kyub.core-1.0.0

in manifest.json you can add it like

"dependencies": {
    "com.kyub.core": "https://gitlab-ci-token:zF563oCqvXdJmiDuMShq@gitlab.com/KyubInteractive/kyublibs.git#com.kyub.core"
  },
