@echo off
:: Ignore the submodule path for local commits
:: Note: Always use double quotes for paths with spaces!
git update-index --no-skip-worktree "bin/Third Party Assets/SimpleJSON/source"
git update-index --no-skip-worktree "MiniScript/.hidden/.source"

echo Submodule tracking has been restored. 
echo Git will now show changes if the submodule version changes.
pause