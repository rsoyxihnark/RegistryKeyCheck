# Changelog

## 2.9.1

- Fixed the list losing its sort order when you type in the Find box, press Compliant or Non-Compliant, or run Query Registry.

## 2.9.0

- The Compliant and Non-Compliant buttons can now both be on at once, showing the rows of both.
- Shown and Total at the bottom left are now the number of lines, with compliant in green and noncompliant in red.
- The Find box is now always open and has the keyboard focus when RegistryKey Check opens.
- Esc in the Find box now clears it and leaves it open.
- All Filters Reset now clears the Find box and leaves it open.
- Ignore.txt is now made only when it has a line in it, and is removed when the last line leaves the Ignore List.
- Open Ignore.txt no longer makes an empty Ignore.txt by itself.
- An empty Ignore.txt is removed when RegistryKey Check opens.

## 2.8.2

- Fixed a lone % in a List.txt line, such as in search?q=%s, being kept although a batch file drops it.
- A %% in a List.txt line is now read as one %, so Expected shows one % and Execute Missing Commands writes one.
- A variable such as %USERPROFILE% in a List.txt line is now filled in anywhere in the line, and one that is not set is left out.
- A %1 to %9 or %* in a List.txt line is now left out, the same as in a batch file run without arguments.
- %SystemRoot% in REG_EXPAND_SZ data is now filled in like a batch file does, so write %%SystemRoot%% to keep the variable.
- ^%SystemRoot^% in a List.txt line is now read the way a batch file reads it, where ^ does not keep the variable.

## 2.8.1

- Fixed a List syntax warning on lines that write into a key or value after an earlier reg delete line resets it.
- Fixed a reg delete line that resets a key staying Non-Compliant after the lines below it wrote into that key.
- Fixed Execute Missing Commands deleting values that later lines had written when it ran a reg delete line again.
- Details now says Key has values or subkeys that no later line writes when a reset key holds anything else.
- The All Filters Reset button is now taller and fills the space below it.
- Log.txt is now made only when there is something to write in it.
- An empty Log.txt left by an older version is removed when RegistryKey Check opens.

## 2.8.0

- The Apply False Filter and Apply True Filter buttons are now called Non-Compliant and Compliant.
- The Non-Compliant button lights red and the Compliant button lights green while its filter is on.
- Pressing a lit Non-Compliant or Compliant button again turns its filter off and shows every row.
- The Help button and the Help window are gone.

## 2.7.1

- Details text is now green for a row that matches.
- The Query RegEdit button is now called Query Registry.
- Details now says Registry instead of RegEdit, such as Key not found in Registry.
- The Help window now says Registry instead of RegEdit and explains the green Details text.
- Log.txt now says Registry instead of RegEdit for a base key or value type that the Registry does not have.

## 2.7.0

- RegistryKey Check, the Detail window and the Help window now have a dark look.
- Rows in the list are no longer colored red, green, grey or yellow.
- Details text is now red for a row that does not match, and orange for a row with a List syntax warning.
- The row under the mouse is now highlighted.
- The Full Path, List Value Name and Compliance Details columns are now called Path, Value and Details.
- The List Value Data and RegEdit Value Data columns are now called Expected and Actual.
- The RegEdit Value Type and List Match with RegEdit columns are now called Registry Value Type and List Match with Registry.
- List Command, List Value Type and Registry Value Type are now hidden until you tick them in Columns.
- The Detail window now uses the new column names, such as Path, Expected and Actual.
- The filter boxes above the columns are gone.
- Ctrl+F opens a Find box that shows only the rows with that text in a shown column, and Esc closes it.
- All Filters Reset now also clears and closes the Find box.
- The Columns menu now closes after you tick or untick a column.
- The Non RegistryKey Commands tab no longer shows a grey strip at the left of each row.
- The Help window now explains Ctrl+F and the red and orange Details text.

## 2.6.0

- Opening RegistryKey Check now runs Read List.txt, Query RegEdit and Apply False Filter by itself when List.txt is beside it.
- Columns now start wide enough to show each header in full, with more room for Full Path, List Value Name and Compliance Details.

## 2.5.1

- Read List.txt, Add to Ignore List and Remove from Ignore List now run faster on long lists.
- List Value Data and RegEdit Value Data now show REG_EXPAND_SZ data as written, such as %SystemRoot%\system32 instead of C:\Windows\system32.
- Hiding a column in Columns now clears its filter box, so no hidden filter keeps rows out of the list.
- A reg line that runs a second command after &, && or || now shows on the Non RegistryKey Commands tab and is never run.
- Fixed reg delete with two backslashes right after the base key deleting everything under that base key.
- Fixed a key path ending in two backslashes being run although reg.exe refuses it.
- Fixed a key path with a backslash right after the base key writing into or deleting from the base key itself.
- Fixed reg delete with /t, /d or /s deleting the key or value although reg.exe refuses it.
- Fixed reg lines that give a switch twice, or both /v and /ve, or both /reg:32 and /reg:64, being run although reg.exe refuses them.
- Fixed a REG_MULTI_SZ line with a /s longer than one character being run although reg.exe refuses it.
- Fixed reg add with /t or /d but no /v or /ve checking and writing only the key instead of its (Default) value.
- Fixed a REG_QWORD line without /d never matching RegEdit and showing Error instead of writing 0 like reg.exe does.
- Fixed a REG_DWORD line with negative data, such as -1, being written although reg.exe refuses it.
- Fixed Execute Missing Commands writing REG_MULTI_SZ data with an empty string in it, such as a\0\0b, although reg.exe refuses it.
- Fixed empty REG_MULTI_SZ data written by Execute Missing Commands being two bytes longer than what reg.exe writes.
- Fixed REG_MULTI_SZ data split with /s also being split at the text \0, such as in /s "#" /d "two\0#strings".
- Fixed a ^ outside quotes, such as in ^%SystemRoot^%, being read as part of the text instead of the way cmd reads it.
- Fixed a variable such as %ProgramFiles% in REG_SZ, REG_MULTI_SZ or REG_NONE data not being filled in the way cmd fills it in.

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
