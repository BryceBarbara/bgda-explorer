/*  Copyright (C) 2011 Ian Brown

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
package net.ijbrown.bgtools.lmp;

import java.io.*;

/**
 * Decodes an xxx.world file.
 */
public class WorldDecode
{
    public static void main(String[] args) throws IOException
    {
//        String outDir = "/emu/bgda/BG/DATA_extracted/test/test_lmp/";
//        String outDir = "/emu/bgda/BG/DATA_extracted/tavern/tavern_lmp/";
        String outDir = "/emu/bgda/BG/DATA_extracted/cellar1/cellar1_lmp/";

        File outDirFile = new File(outDir);
        outDirFile.mkdirs();

        WorldDecode obj = new WorldDecode();
        obj.read("cellar1.world", outDirFile);
        String txt = obj.disassemble();
        obj.writeFile("cellar1.world.txt", outDirFile, txt);
    }

    private void writeFile(String filename, File outDirFile, String txt) throws IOException
    {
        File file = new File(outDirFile, filename);
        PrintWriter writer = new PrintWriter(file);
        writer.print(txt);
        writer.close();
    }

    private int fileLength;
    private byte[] fileData;

    private void read(String filename, File outDirFile) throws IOException
    {
        File file = new File(outDirFile, filename);
        BufferedInputStream is = new BufferedInputStream(new FileInputStream(file));

        fileLength = (int) file.length();
        fileData = new byte[fileLength];

        int offset = 0;
        int remaining = fileLength;
        while (remaining > 0) {
            int read = is.read(fileData, offset, remaining);
            if (read == -1) {
                throw new IOException("Read less bytes then expected when reading file");
            }
            remaining -= read;
            offset += read;
        }
    }

    private String disassemble()
    {
        StringBuilder sb = new StringBuilder();

        int numElements = DataUtil.getLEInt(fileData, 0);
        sb.append("Num Elements: ").append(HexUtil.formatHex(numElements)).append("\r\n");


        int offset4 = DataUtil.getLEInt(fileData, 0x4);
        sb.append("Offset4: ").append(HexUtil.formatHex(offset4)).append("\r\n");

        sb.append("\r\n");

        int rows = DataUtil.getLEInt(fileData, 0x10);
        int cols = DataUtil.getLEInt(fileData,0x14);

        sb.append("Rows: ").append(rows).append("\r\n");
        sb.append("Cols: ").append(cols).append("\r\n");

        int offset18 = DataUtil.getLEInt(fileData, 0x18);
        sb.append("Offset18: ").append(HexUtil.formatHex(offset18)).append("\r\n");
        // This is an array of 4 byte offsets
        // Each offset points to a -1 terminated array of shorts

        sb.append("\r\n");

        int elementBase = DataUtil.getLEInt(fileData, 0x24);
        sb.append("Element Base: ").append(HexUtil.formatHex(elementBase)).append("\r\n");

        sb.append("\r\n");
                

        int rows1 = DataUtil.getLEInt(fileData, 0x30);
        int cols1 = DataUtil.getLEInt(fileData,0x34);

        sb.append("Rows1: ").append(rows1).append("\r\n");
        sb.append("Cols1: ").append(cols1).append("\r\n");
        int offset38 = DataUtil.getLEInt(fileData, 0x38);
        sb.append("Offset38: ").append(HexUtil.formatHex(offset38)).append("\r\n");

        sb.append("-----------------------------------------------------\r\n");
        sb.append("\r\n");
        sb.append("Offsets array \r\n \r\n");
        for (int i=0; i<rows*cols; ++i){
            int off = DataUtil.getLEInt(fileData, offset18 + i*4);
            sb.append(i).append(" : ").append(HexUtil.formatHex(off)).append(" -> ");

            int u = DataUtil.getLEShort(fileData, off);
            while (u >= 0){
                sb.append(u);
                off += 2;
                u = DataUtil.getLEShort(fileData, off);
                if (u >= 0){
                    sb.append(", ");
                }
            }

            sb.append("\r\n");
        }

        return sb.toString();
    }

}