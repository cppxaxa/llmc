Features:
- Rewrite500(repo="samplebigrepo_changes_500",wildcard="*.*")
Prompt:
- IncludeDateTime
- IncludeLs
PreBuild:
- DeleteFolder(folder="samplerepo_changes_500")
- CopyFolder(source="samplebigrepo",destination="samplebigrepo_changes_500")
---
Give instructions to write complete README.md with minimal changes.
I want you to revise the README.md to fix the TODO as well as add a section of "Steps to execute".
Also read any *.py file to understand better the library used.
But write to only one file = README.md
