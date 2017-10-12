set -x
ulimit -n 4096

echo "TRAVIS_EVENT_TYPE value $TRAVIS_EVENT_TYPE"


git submodule update --init


powershell -c "cd src; ./bootstrap.ps1; ./build.ps1 -framework "netcoreapp2.0" Release"

sudo powershell -c "Import-Module ./tools/setup.psm1; Invoke-SHiPSTest"
