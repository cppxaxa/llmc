Features:
- Rewrite100(repo="samplerepo_changes",wildcard="*.*")
Prompt:
- IncludeDateTime
- IncludeLs
PreBuild:
- DeleteFolder(folder="samplerepo_changes")
- CopyFolder(source="samplerepo",destination="samplerepo_changes")
---
Give instructions to write complete README.md with minimal changes.
I want you to revise the README.md to fix the TODO as well as add a section of "Steps to execute".
Also read any *.py file to better form contents of README.md
But write to only one file = README.md
