@echo off

for /f "tokens=1,2 delims=:," %%a in ('findstr "version" "package.json"') do ( set version=%%b&goto :break1)
:break1
for /f "tokens=1,2 delims=:," %%a in ('findstr "name" "package.json"') do ( set name=%%b&goto :break2)
:break2

set name=%name:"=%
set name=%name: =%
set version=%version:"=%
set version=%version: =%
set branchTag=%name%-%version%

set gitRootPath=%cd:Assets\=,%
FOR /f "tokens=1,2 delims=," %%a IN ("%gitRootPath%") do ( set relativePath=Assets\%%b&set gitRootPath=%%a&goto :break3)
:break3

@echo on

cd "%gitRootPath%"
git subtree split --prefix="%relativePath:\=/%" --branch %name%
git tag "%branchTag%" %name%
git rm --cached ".\%relativePath%\release-push.cmd"
git rm --cached ".\%relativePath%\release-push.cmd.meta"
git push origin %name% --tags

SET /p exit=Press any key to exit