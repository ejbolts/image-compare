# Image Compare

Double-click **ImageCompare.exe**. No installer, account, or internet connection is needed. Uses the .NET Framework included with Windows 10 and 11.

1. Click **Open before…** and **Open after…** to choose your images, or drag two image files onto the comparison area.
2. Drag the white divider to reveal the changes.
3. Check **Vertical comparison** (or press **V**) to switch between horizontal (side to side) and vertical (up and down) split.
4. Use **Swap images** to reverse them, or **Center slider** to return to 50%.

Click **Try demo** to experiment with a built-in pair of illustrations.

When the comparison is focused, arrow keys (Left/Right or Up/Down) adjust by 1%, Shift+Arrow keys adjust by 10%, and Home/End reveals a whole image. Press **V** at any time to toggle between horizontal and vertical comparison. Resize or maximize the window for a larger view.

Supports PNG, JPEG, BMP, GIF (first frame), and TIFF (first page). Images are only read, never modified or uploaded. Files are released immediately after loading. Transparent areas show a checkerboard. Different image sizes are centered on a common canvas at the same scale without stretching; matching size and crop gives the best comparison.

Drop one file onto a picker to replace that image. Drop two files onto the comparison area to load both (use Swap if their order is reversed). Dropping a single file onto the canvas replaces the image corresponding to that half (left/top for before, right/bottom for after). The app can also accept two file paths as command-line arguments. Selections are not saved after closing.

Source and build script are included. To rebuild, run `powershell -ExecutionPolicy Bypass -File .\build.ps1` in this folder. No external packages are needed.
