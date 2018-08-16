set -x
ulimit -n 4096

echo "TRAVIS_EVENT_TYPE value $TRAVIS_EVENT_TYPE"


git submodule update --init


pwsh  -c "cd src; ./bootstrap.ps1; ./build.ps1 -framework "netstandard2.0" Release"

sudo pwsh  -c "Import-Module ./tools/setup.psm1; Invoke-SHiPSTest"
