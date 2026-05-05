@echo off
:: Initialize the submodule
::git submodule update --init --recursive

:: Ignore the submodule path for local commits
:: Note: Always use double quotes for paths with spaces!
git update-index --skip-worktree "bin/Third Party Assets/SimpleJSON/source"
git update-index --skip-worktree "MiniScript/.hidden/.source"

echo Submodule is now set to skip-worktree.
pause