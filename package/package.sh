#!/bin/bash
set -euo pipefail

# Stage only the DLL from the current build. The staging directory is ignored
# by git, so remove stale DLLs explicitly before creating the package.
mkdir -p skel/plugins
rm -f skel/plugins/*.dll
cp ../UpgradeEveryRound/bin/Release/netstandard2.1/UpgradeEveryRound.dll skel/plugins/UpgradeEveryRound.dll
cp ../CHANGELOG.md ../LICENSE.txt ../README.md skel

#remove previous package
if [ -f UpgradeEveryRound.zip ] ; then
  rm -vf UpgradeEveryRound.zip
fi

#package skeleton
pushd skel
zip -r9 ../UpgradeEveryRound.zip *
popd
