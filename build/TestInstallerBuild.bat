call "c:\Program Files (x86)\Microsoft Visual Studio 9.0\VC\vcvarsall.bat"

pushd c:\dev\myWorkSafe\build
MSbuild /target:installer /property:teamcity_build_checkoutDir=c:\dev\myWorkSafe /verbosity:detailed /property:teamcity_dotnet_nunitlauncher_msbuild_task="notthere" /property:BUILD_NUMBER="*.*.0.1" /property:Minor="1"

pushd c:\dev\myWorkSafe\build
setupbld -title "myWorkSafe" -mpsu ..\output\installer\myWorkSafeInstaller.0.1.0.msi ..\lib\Synchronization-v2.0-x86-ENU.msi ..\lib\ProviderServices-v2.0-x86-ENU.msi -setup setup.exe -out ..\output\installer\myWorkSafeInstaller.0.1.0.exe
popd
popd
PAUSE