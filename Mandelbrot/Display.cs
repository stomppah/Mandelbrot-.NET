﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Mandelbrot
{

    public partial class Display : Form
    {
        private int MAX = 512;      // max iterations
        private const double SX = -2.025; // start value real
        private const double SY = -1.125; // start value imaginary
        private const double EX = 0.6;    // end value real
        private const double EY = 1.125;  // end value imaginary
        private static int x1, y1, xs, ys, xe, ye;
        private static double xstart, ystart, xende, yende, xzoom, yzoom;
        private static bool action, rectangle, finished;
        private static float xy;

        private Bitmap offScreen, offScreenIndexed;
        private ColorPalette palette;
        private Graphics g1;
        private Pen p;
        private State state;
        private List<State> zoomLevels = new List<State>();
        private int saveSlot = 1;


        public void init() // all instances will be prepared
        {
            finished = false;
            action = false;
            p = new Pen(Color.Black);
            setZoomLevel();
            finished = true;
        }

        // pulled this out to allow me to explictly set the zoom level - used for window resize changes an
        private void setZoomLevel()
        {
            x1 = Width;
            y1 = Height;

            xy = (float)x1 / (float)y1;

            try
            {
                offScreen = new Bitmap(picture.Width, picture.Height); //picture = createImage(x1, y1);
            }
            //catches an error when window is minimized
            catch (ArgumentException e) 
            { 
                Console.WriteLine(e.StackTrace); 
            }

            g1 = Graphics.FromImage(offScreen);
        }

        public void start()
        {
            action = false;
            rectangle = false;
            initvalues();
            xzoom = (xende - xstart) / (double)x1;
            yzoom = (yende - ystart) / (double)y1;

            zoomLevels.Add(new State(xstart, ystart, xende, yende));           //begin route trace -- to allow for zooming out
            
            mandelbrot();
        }

        private void mandelbrot() // calculate all points
        {
            int x, y;
            float h, b, alt = 0.0f;

            action = false;
            /* setCursor(c1); */
            Text = "Mandelbrot-Set will be produced - please wait...";
            for (x = 0; x < x1; x += 2)
                for (y = 0; y < y1; y++)
                {
                    h = pointcolour(xstart + xzoom * (double)x, ystart + yzoom * (double)y); // color value
                    if (h != alt)
                    {
                        b = 1.0f - h * h; // brightnes
                        ///djm added
                        ///HSBcol.fromHSB(h,0.8f,b); //convert hsb to rgb then make a Java Color
                        ///Color col = new Color(0,HSBcol.rChan,HSBcol.gChan,HSBcol.bChan);
                        ///g1.setColor(col);
                        //djm end
                        //djm added to convert to RGB from HSB

                        //g1.Clear((Color)HSBColor.FromHSB(new HSBColor(h, 0.8f, b)));     //Color(HSBColor.FromHSB(h, 0.8f, b));
                        //djm test
                        Mandelbrot.HSBColor hsb = new Mandelbrot.HSBColor(h * 255f, 0.8f * 255f, b * 255f);

                        Color col = hsb.Color;
                        int red = col.R;
                        int green = col.G;
                        int blue = col.B;

                        alt = h;
                        p.Color = Color.FromArgb(red, green, blue);
                    }
                    g1.DrawLine(p, x, y, x + 1, y);
                }

            Text = "Mandelbrot-Set ready - please select zoom area with pressed mouse.";
        }

        private float pointcolour(double xwert, double ywert) // color value from 0.0 to 1.0 by iterations
        {
            double r = 0.0, i = 0.0, m = 0.0;
            int j = 0;

            while ((j < MAX) && (m < 4.0))
            {
                j++;
                m = r * r - i * i;
                i = 2.0 * r * i + ywert;
                r = m + xwert;
            }
            return (float)j / (float)MAX;
        }

        private void initvalues() // reset start values
        {
            xstart = SX;
            ystart = SY;
            xende = EX;
            yende = EY;
            if ((float)((xende - xstart) / (yende - ystart)) != xy)
                xstart = xende - (yende - ystart) * (double)xy;
        }

        /*
         * Contructor
         * */
        public Display()
        {
            InitializeComponent();
            init();
            state = new State(xstart, ystart, xende, yende);
            start();
        }

        private void pictureBoxPaint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.DrawImage(colourCycleTimer.Enabled ? offScreenIndexed : offScreen, 0, 0);      //(picture, 0, 0, this);

            if (rectangle)
            {
                p.Color = Color.Black;      //.setColor(Color.white);
                if (xs < xe)
                {
                    if (ys < ye) g.DrawRectangle(p, xs, ys, (xe - xs), (ye - ys));  //drawRect(xs, ys, (xe - xs), (ye - ys));
                    else g.DrawRectangle(p, xs, ye, (xe - xs), (ys - ye));
                }
                else
                {
                    if (ys < ye) g.DrawRectangle(p, xe, ys, (xs - xe), (ye - ys));          //drawRect(xe, ys, (xs - xe), (ye - ys));
                    else g.DrawRectangle(p, xe, ye, (xs - xe), (ys - ye));
                }
            }
        }

        private void mousePressed(object sender, MouseEventArgs e)
        {
            //e.consume();
            action = (e.Button == MouseButtons.Right) ? false : true;
            if (action)
            {
                xs = e.X;
                ys = e.Y;
            }
            
        }

        private void mouseReleased(object sender, MouseEventArgs e)
        {
            int z, w;

            //e.consume();
            if (action)
            {
                xe = e.X;
                ye = e.Y;
                if (xs > xe)
                {
                    z = xs;
                    xs = xe;
                    xe = z;
                }
                if (ys > ye)
                {
                    z = ys;
                    ys = ye;
                    ye = z;
                }
                w = (xe - xs);
                z = (ye - ys);
                if ((w < 2) && (z < 2)) initvalues();
                else
                {
                    if (((float)w > (float)z * xy)) ye = (int)((float)ys + (float)w / xy);
                    else xe = (int)((float)xs + (float)z * xy);
                    xende = xstart + xzoom * (double)xe;
                    yende = ystart + yzoom * (double)ye;
                    xstart += xzoom * (double)xs;
                    ystart += yzoom * (double)ys;
                }
                xzoom = (xende - xstart) / (double)x1;
                yzoom = (yende - ystart) / (double)y1;

                //if zooming all the way out, clear the list
                // * only works if the window is kept at it's initial size
                // * not been able to work out a  more robust method
                if (xzoom == 0.0033936652847949196 && yzoom == 0.0033936651583710408)
                {
                    for (int i = 0; i < zoomLevels.Count - 1; i++) 
                    {
                        zoomLevels.RemoveAt(i);
                    }
                    
                }
                zoomLevels.Add(new State(xstart, ystart, xende, yende));

                mandelbrot();
                rectangle = false;
                Refresh();      //repaint();

                offScreenIndexed = null;
            }
            action = false;
        }

        private void mouseDragged(object sender, MouseEventArgs e)
        {
            //e.consume();
            if (action)
            {
                xe = e.X;
                ye = e.Y;
                rectangle = true;
                Refresh();      //repaint();
            }
        }

        private void copyToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetImage(offScreen);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string saveFile = "";
            saveImageDialog.Title = "Save current display to image file";
            saveImageDialog.FileName = "";

            saveImageDialog.Filter = "Bitmap Files|*.bmp";

            if (saveImageDialog.ShowDialog() != DialogResult.Cancel)
            {
                saveFile = saveImageDialog.FileName;
                offScreen.Save(saveFile, System.Drawing.Imaging.ImageFormat.Bmp);
            }
        }

        /*
         * Saves current zoomed position to file
         * */
        private void quicksaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            state.SetValues(xstart, ystart, xende, yende);
            state.QuickSave(saveSlot);
            quickloadToolStripMenuItem.Enabled = true;
        }

        /*
         * Loads saved state from file and sets relevant globals
         * */
        private void quickloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                state.QuickLoad(saveSlot);

                //disable colour cycling - wont work if enabled
                colourCycleTimer.Enabled = false;
                cycleColoursToolStripMenuItem.Checked = false;

                //remove indexed image - important
                offScreenIndexed = null;

                xstart = state.xstart;
                ystart = state.ystart;

                xende = state.xende;
                yende = state.yende;

                xzoom = (xende - xstart) / (double)x1;
                yzoom = (yende - ystart) / (double)y1;

                //reset list
                for (int i = 0; i < zoomLevels.Count - 1; i++)
                {
                    zoomLevels.RemoveAt(i);
                }

                zoomLevels.Add(new State(xstart, ystart, xende, yende));

                refreshFractal();
            }
            catch (FileNotFoundException error)
            {
                Console.WriteLine(error.StackTrace);
            }
        }

        /*
         * Enables/Disables Colour Cycling
         * */
        private void cycleColoursMainMenu_Click(object sender, EventArgs e)
        {
            colourCycleTimer.Enabled = !colourCycleTimer.Enabled ? true : false;
            cycleColoursToolStripMenuItem.Checked = !cycleColoursToolStripMenuItem.Checked ? true : false;
        }

        /*
         * Ticker used to cycle the colour palette
         * */
        private void colourCycleTimer_Tick(object sender, EventArgs e) 
        {
            offScreenIndexed = offScreenIndexed == null ? offScreen.Clone(new Rectangle(0, 0, picture.Width, picture.Height), PixelFormat.Format8bppIndexed) : offScreenIndexed;
            palette = offScreenIndexed.Palette;

            // base the default entry on the changing palette - stops the centre of the fractal remaining black
            palette.Entries[0] = HSBColor.ShiftHue((Color)palette.Entries[1], 1);

            for (int i = 1; i < palette.Entries.Length; i++)
            {
                palette.Entries[i] = HSBColor.ShiftHue((Color)palette.Entries[i], 1);
            }
            offScreenIndexed.Palette = palette;

            Refresh();
        }

        /*
         * Redraws and resizes the fractal baed on new window size
         * */
        private void Display_Resize(object sender, EventArgs e)
        {
            setZoomLevel();
            xzoom = (xende - xstart) / (double)x1;
            yzoom = (yende - ystart) / (double)y1;

            mandelbrot();

            try
            {
                // reset colour index
                offScreenIndexed = offScreen.Clone(new Rectangle(0, 0, picture.Width, picture.Height), PixelFormat.Format8bppIndexed);
            }
            //catches an error when window is minimized
            catch (ArgumentException error)
            {
                Console.WriteLine(error.StackTrace);
            }

            Refresh();
        }

        private void slot1MenuItem_Click(object sender, EventArgs e)
        {
            slot1MenuItem.Checked = !slot1MenuItem.Checked ? true : false;
            slot2MenuItem.Checked = !slot2MenuItem.Checked ? true : false;
            saveSlot = 1;
        }

        private void slot2MenuItem_Click(object sender, EventArgs e)
        {
            slot1MenuItem.Checked = !slot1MenuItem.Checked ? true : false;
            slot2MenuItem.Checked = !slot2MenuItem.Checked ? true : false;
            saveSlot = 2;
        }

        /*
         * Called when a keypress is detected by the system
         * */
        private void Display_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar.ToString())
            {
                    /*
                     * Zooms out to the previous point, and removes the last postion from the stack.
                     * Does not work correctly while colour cycling is enabled.
                     * */
                case "-":
                    State tmp = (State)zoomLevels.ElementAt(zoomLevels.Count - 2);;

                    //disable colour cycling - wont work if enabled
                    colourCycleTimer.Enabled = false;
                    cycleColoursToolStripMenuItem.Checked = false;

                    //remove indexed image - important
                    offScreenIndexed = null;
                    
                    //dont remove from list if only one state is present
                    if (zoomLevels.Count > 1)
                        zoomLevels.RemoveAt(zoomLevels.Count - 1);

                    xstart = tmp.xstart;
                    ystart = tmp.ystart;

                    xende = tmp.xende;
                    yende = tmp.yende;

                    xzoom = (xende - xstart) / (double)x1;
                    yzoom = (yende - ystart) / (double)y1;

                    refreshFractal();

                    break;
            }
        }

        private void maxIterationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            maxIterationsToolStripMenuItem.Checked = !maxIterationsToolStripMenuItem.Checked ? true : false;
            maxIterationsSlider.Visible = !maxIterationsSlider.Visible ? true : false;
        }

        private void maxIterations_Scroll(object sender, EventArgs e)
        {
            //disable colour cycling - wont work if enabled
            colourCycleTimer.Enabled = false;
            cycleColoursToolStripMenuItem.Checked = false;

            //remove indexed image - important
            offScreenIndexed = null;

            MAX = maxIterationsSlider.Value;
            refreshFractal();

        }

        private void refreshFractal()
        {
            mandelbrot();
            Refresh();
        }
    }

}