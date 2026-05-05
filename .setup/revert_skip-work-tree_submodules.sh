#!/bin/bash
# Initialize submodules
git submodule update --init --recursive

# Mark the submodule to be ignored locally for commits
git update-index --no-skip-worktree "bin/Third Party Assets/SimpleJSON/source"
git update-index --no-skip-worktree "MiniScript/.hidden/.source"