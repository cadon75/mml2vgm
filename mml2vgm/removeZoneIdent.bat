@echo off
echo zip,dll,exe�t�@�C����Zone���ʎq���폜���܂��B
pause

echo on
FOR %%a in (*.zip *.dll *.exe) do (echo . > %%a:Zone.Identifier)
FOR %%a in (mml2vgm\*.zip mml2vgm\*.dll mml2vgm\*.exe) do (echo . > %%a:Zone.Identifier)
FOR %%a in (mml2vgmIDE\*.zip mml2vgmIDE\*.dll mml2vgmIDE\*.exe) do (echo . > %%a:Zone.Identifier)
@echo off

echo �������܂����B
pause
echo on
