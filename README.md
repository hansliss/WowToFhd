# WowToFhd
Convert SparkMaker .wow print files to SparkMaker FHD .fhd format

This is an extremely crude hack to convert .wow files to .fhd format, for anyone who has a use for it.

To use it, click the "Infile" textbox and select a .wow input file, and then click the "Outfile" textbox and select a destination .fhd file, and click "Process". If you want the program to create PNG image files of each frame, in both original and converted form, check the checkbox *before* clicking "Process". The images will end up in the same directory as the output file, and using its filename as a prefix.

I have only tested this insofar as the resulting file can be opened by ChituBox, and looks correct.

The main potential problem is the format of the embedded PNG images in the FHD file. I took pains to make sure they are the same 8-bit grayscale images as ChituBox produces, but there's no way of knowing, apart from doing a test print, that none of the many minor details that I *can't* control differs somehow and prohibits the SparkMaker's firmware from parsing them.

Let me know if you test this with an actual printer - I can't be bothered to set mine up just to test it. Also let me know if you find a bug (not that you will, my code is of course bug-free), and let me know if it works for you and you have some use for it.

I've tried to comment the code. It turns out this would have been quite a bit clearer if i had written it in C on a Linux system. C#/.NET has too many weird behaviours and limited options in its standard libraries to allow this to be done easily. Consequently, you may also see some of this code as example code for how to do certain things.