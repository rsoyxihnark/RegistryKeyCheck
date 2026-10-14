# Changelog

## 2.2.0

- A colored bar at the left of each row shows whether it is True, False, Unknown or Ignored.
- When a row is False, the RegEdit Value Data or RegEdit Value Type that differs is shown in red.
- List Value Data and RegEdit Value Data use a fixed-width font, so hex values line up.
- Execute Result shows Error in red and successful in green.
- The tabs show how many rows they hold, and Execute Selected shows how many rows are selected.
- The Detail window shows the RegEdit value that differs in red.
- Fixed reg delete with /va deleting the whole key with its subkeys instead of only its values.
- Fixed REG_EXPAND_SZ data with variables other than %SystemRoot%, such as %ProgramFiles%, never matching RegEdit.
- Fixed a row with a misspelled base key, such as HKEY_LOCAL_MACHIN, showing successful after it was executed.
- Fixed the Select box in the column header not showing whether the shown rows are selected.

## 2.1.0

- RegistryKey Check has a new dark look, with a toolbar on top, a search bar and a status bar that shows progress.
- The search bar searches every column at once, and Ctrl+F jumps to it.
- Every column header now has its own filter box.
- The List BaseKey and List SubKey columns are now one Path column that shows the full key path.
- Empty fields now show as blank instead of This_field_is_NULL.
- The default value of a key now shows as (Default) in List Value Name.
- The True, False, Unknown and Ignored buttons in the status bar show or hide rows of that status and show how many there are.
- Clear filters clears the search and every column filter and shows every status again.
- List Match with RegEdit now shows True, False, Unknown and Ignored as colored labels.
- Tick the Select box of rows and press Execute Selected to run the reg commands of only those rows.
- Go to line jumps to a line of List.txt, and Ctrl+G jumps to it.
- Right-click a row to copy its reg command, key path or value name.
- Right-click a row and choose Show Details, or press Enter, to open the Detail window.
- Space selects or unselects the highlighted rows for Execute Selected.
- Jump to RegEdit now closes RegEdit first, so it opens at the key even when RegEdit is already open.
- Query RegEdit and Execute Missing Commands now show their progress and run faster on long lists.
- F5 runs Query RegEdit, Ctrl+O runs Read List.txt and F1 opens Help.
- The Detail window now has Jump to RegEdit and Copy reg command buttons.
- An empty list now says how to load the reg commands in List.txt.
- The Help window now has a Close button and closes with Esc.

## 2.0.1

- Right-click a column header or press Columns to hide or show any column, such as SubKey Available? or List Match with RegEdit.
- Right-click a row and choose Jump to RegEdit to open RegEdit at the key of that row.
- Right-click rows to add them to or remove them from the Ignore List, which is kept in Ignore.txt beside List.txt.
- Rows on the Ignore List show as Ignored in grey and are skipped by Query RegEdit and Execute Missing Commands.
- Open Ignore.txt opens the Ignore List in Notepad.
- RegistryKey Check now remembers its window size and position and the width, order and visibility of each column.
- The Detail window now closes with Esc.
- REG_DWORD and REG_QWORD data now shows in hex and decimal, the same way RegEdit shows it.
- Errors in Log.txt now show the line of RegistryKey Check where they happened, without any folder from the developer's computer.
- Fixed REG_DWORD values above 0x7fffffff showing as negative numbers.
- Fixed REG_DWORD data longer than 8 hex digits crashing Read List.txt.
- Fixed REG_QWORD data written in hex, such as 0x10, never matching RegEdit.
- Fixed value names in a different upper or lower case than in RegEdit showing as Not Available and False.
- Fixed a base key written in lower case, such as hkey_local_machine, showing as Not Available.
- Text data that differs from RegEdit only in upper or lower case now counts as a match.
- Fixed /d data containing quotes, such as "regedit.exe "%1"", being read differently from what reg.exe writes.
- Fixed two or more spaces in a row inside /d data being read as one space.
- Fixed /V, /T and /D written in upper case being ignored.
- Fixed a key path written without quotes not being read.
- Fixed Query RegEdit keeping old RegEdit data on a row after the value or key was removed.
- Fixed clicking an empty part of the list not clearing the selected rows.

## 2.0.0

- RegistryKey Check now comes as a single exe, with no DLL files to keep beside it.
- RegistryKey Check now runs on .NET 10 and needs Windows 10 or later.
- Fixed the Help window showing version 1.0.1 instead of the version you are running.
- Fixed Execute Missing Commands refreshing the list before all the commands had run.
- Fixed REG_BINARY values being written and checked as text instead of as the hex bytes written in List.txt.
- Fixed Read List.txt crashing on a REG_MULTI_SZ line with only one character of data.
- Fixed a lowercase type such as reg_dword not being recognised when the /v name had no quotes.
