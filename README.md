# croCKer
Program that lets the user read and write .SRP and .VAR files used in games for the Tmr-Hiro ADV System (mostly Marigold produced games).

### Notes on its usage
1. For now, the files will be exported solely as text files. But if there's enough demand, exporting as JSON files can be done.

### How are SRP and VAR files structured?
While the code also documents how both formats are structured, here it is also the same information on a more accessible manner:
SRP files are divided into 2 parts:
  * **Header**: Number of instructions (4 bytes)
    * Number of files: each instruction is an entry, so we calculate how many of them the SRP file contains. The game engine does NOT follow this value, it can be completely wrong since it only checks for a specific end of script instruction (more on that later).
  * **Instructions**: The following structure gets repeated with the value obtained before: Instruction length (2 bytes) + Instruction type (2 bytes) + Instruction flags (2 bytes) + Instruction data (`Instruction length` - 2 - 2 bytes)
    * Instruction length: the length of a the instruction in bytes (the result only tells the amount of bytes the type, flags and data fields have).
    * Instruction type: the type of the command. While we can't be 100% sure for all the games, these are the most important ones that we can know based on their context: 0 is for text inside in-game, 1 is for when the player has to choose an option in-game, 2 is for loading images (character sprites of backgrounds) and 3 is for sounds (BGM or SE).
    * Instruction flags: these flags apparently are for adjusting some choices when loading an instruction of a specific type.
    * Instruction data: the bulk of the instruction. If it is text related, it will include the dialog text. Or if it has to load a specific image, here it is where it will make the call. Also something very important, only this section of the file has its **nibbles swapped**.
   
Besides that, the last instruction for any .SRP file is __always__ *0x0310*.
   
VAR files are a bit different, they simply repeat a simple pattern until the penultimate line:
  * **Variable length**: the length of the variable's data (2 bytes)
  * **Variable data**: the variable alongside the name this instruction sets. Always stored as SHIFT-JIS strings.(`Variable length` bytes)

Then, their last line always is `[END]`.
