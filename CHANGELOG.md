# Changelog

## 2.0.0

- RegistryKey Check now comes as a single exe, with no DLL files to keep beside it.
- RegistryKey Check now runs on .NET 10 and needs Windows 10 or later.
- Fixed the Help window showing version 1.0.1 instead of the version you are running.
- Fixed Execute Missing Commands refreshing the list before all the commands had run.
- Fixed REG_BINARY values being written and checked as text instead of as the hex bytes written in List.txt.
- Fixed Read List.txt crashing on a REG_MULTI_SZ line with only one character of data.
- Fixed a lowercase type such as reg_dword not being recognised when the /v name had no quotes.
