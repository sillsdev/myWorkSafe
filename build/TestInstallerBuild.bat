call "c:\Program Files (x86)\Microsoft Visual Studio 10.0\VC\vcvarsall.bat"

pushd c:\dev\myWorkSafe\build
MSbuild /target:installer /property:teamcity_build_checkoutDir=c:\dev\myWorkSafe /verbosity:detailed /property:teamcity_dotnet_nunitlauncher_msbuild_task="notthere" /property:BUILD_NUMBER="*.*.0.1" /property:Minor="1"
pushd c:\dev\myWorkSafe\build
popd
PAUSE