# Changelog

## 2.5.0

- Empty fields now show as blank cells instead of This_field_is_NULL, in the list and in the Detail window.
- The ID and List ID columns are now one UID column, which shows the line of List.txt each row comes from, on both tabs.
- The List BaseKey and List SubKey columns are now one Full Path column that shows the whole key path.
- Execute Result, List Match with RegEdit, Value Name Available? and SubKey Available? are now hidden until you tick them in Columns.
- The Detail window now shows UID and Full Path in place of ID, List ID, List BaseKey and List SubKey.
- Log.txt now notes a column whose saved layout in Settings.json no longer applies.

## 2.4.0

- The new Compliance Details column says Compliant for a row that matches RegEdit, and why a row does not, such as Value data differs.
- A row that changes the same key or value as another line shows in yellow, with a List syntax warning in Compliance Details.
- The Detail window now shows Compliance Details.
- The Help window now explains Compliance Details and the yellow rows.
- Lines that start with reg.exe, @reg or the full path of reg.exe in System32 are now read the same as lines that start with reg.
- A reg line with a switch or word that reg add or reg delete does not take now shows on the Non RegistryKey Commands tab and is never run.
- Execute Missing Commands now closes each registry key it opens as soon as it is done with it.
- Execute Missing Commands no longer pauses for a moment between its steps.
- Fixed a key path with spaces written without quotes making reg delete or reg add act on the part of the path before the space.
- Fixed reg delete with a misspelled switch, such as /vv, or /v with no name after it, deleting the whole key.
- Fixed reg delete with both /va and /v deleting every value of the key.
- Fixed a quoted key path ending in a backslash creating a key with the rest of the line in its name.
- Fixed letters such as ü and ş in a List.txt or Ignore.txt saved as ANSI turning into unreadable characters.
- Fixed Execute Missing Commands showing Error for a reg delete line whose key or value an earlier line had already deleted.
- Fixed a reg delete line with a misspelled base key, such as HKEY_LOCAL_MACHIN, showing True as if the key were already gone.
- Fixed Jump to RegEdit opening the 64-bit key instead of the 32-bit one for a /reg:32 line under HKEY_LOCAL_MACHINE.
- Fixed Jump to RegEdit opening the key of the first selected row instead of the row you right-clicked.
- Fixed the column filter boxes not filtering after text was pasted or cut with the mouse.
- Fixed the row right-click menu staying usable while Query RegEdit or Execute Missing Commands was running.

## 2.3.0

- Query RegEdit now runs faster on long lists.
- Execute Result now stays blank for a row Execute Missing Commands did not have to change.
- The Help window now explains the Ignore List, Jump to RegEdit, Columns and closing the Detail window with Esc.
- The Help window now shows a scroll bar when its text does not fit.
- Fixed reg delete with /va deleting the whole key with its subkeys instead of only its values.
- Fixed /reg:32 and /reg:64 being ignored, so every row was checked and changed in the 64-bit registry.
- Fixed REG_EXPAND_SZ data with variables other than %SystemRoot%, such as %ProgramFiles%, never matching RegEdit.
- Fixed Execute Missing Commands writing out the full path for REG_EXPAND_SZ data instead of the variable, such as %SystemRoot%.
- Fixed REG_MULTI_SZ data split with /s, such as /s ",", being checked and written as one string.
- Fixed REG_NONE data written by Execute Missing Commands being two bytes shorter than what reg.exe writes.
- Fixed Execute Missing Commands saying successful for a row it could not write, such as one with a misspelled base key.
- Fixed a row keeping its old True or False when Query RegEdit could not read its value, instead of showing Unknown.
- Fixed Add to Ignore List and Remove from Ignore List wiping lines you had added to Ignore.txt by hand.
- Fixed a line dropping off the Ignore List when its upper or lower case was changed in List.txt.
- Fixed Read List.txt closing RegistryKey Check when List.txt or Ignore.txt could not be read.
- Fixed RegistryKey Check closing itself at start when List.txt or Log.txt could not be created beside it.
- Fixed Query RegEdit and Execute Missing Commands closing RegistryKey Check when Log.txt could not be written.
- Fixed Open List.txt, Open Log.txt and Open Ignore.txt closing RegistryKey Check when Notepad could not be opened.
- Fixed a column width set by double clicking the line between two headers not coming back when RegistryKey Check opens again.
- Fixed missing spaces and a stray comma in the Help window, the Execute Missing Commands warning and Log.txt.

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
