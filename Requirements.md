# MKV Header Tool

## Project Requirements

### Simple UI:
- File Picker
- Interactive display
- Save button

### Feature descriptions
___File Picker___

Clicking the file picker allows you to navigate to a file on the users device.

Only MKV files are displayed.

Selecting a file does two things. 

1) It stores the directory of the file selected. This is so, the next time the file picker button is clicked, it will navigate to the most recent directory selected from

2) The application attempts to read the header information of the file selected. If it succeeds, it sends the header information to the UI.

___Interactive display___

The display shows a list of headers and their values

For each value there is a checkbox. If the value is set to true, the checkbox is checked, if false it is not.

The user can click the checkboxes to enable and disable any given header value.

___Save button___

If any changes have been made the save button is enabled. Clicking on the save button saves the changes to the selected file.

## MKV Prop Edit documentation reference

https://mkvtoolnix.download/doc/mkvpropedit.html#d4e1073