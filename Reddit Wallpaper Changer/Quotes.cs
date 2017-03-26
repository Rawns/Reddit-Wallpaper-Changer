using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;

//TODO: Some initial work for adding random quotes to wallpapers  

namespace Reddit_Wallpaper_Changer
{
    class Quotes
    {
        public string quoteAuth { get; set; }
        public string authQuote { get; set; }
        public string quote { get; set; }


        //=================================================================================
        // Adds a wallpaper to the sellected wallpaper
        //=================================================================================
        private void addQuote(string wallpaper)
        {
            Logging.LogMessageToFile("Looking for an insightful quote.", 0);
            getQuote();
            addText(wallpaper);
            return; 
        } 

        //=================================================================================
        // Grab a quote from the API to add to the wallpaper
        //=================================================================================
        private void getQuote()
        {
            try
            {
                WebRequest req = WebRequest.Create("http://api.forismatic.com/api/1.0/");
                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";

                string reqString = "method=getQuote&key=457653&format=xml&lang=en";
                byte[] reqData = Encoding.UTF8.GetBytes(reqString);
                req.ContentLength = reqData.Length;

                using (Stream reqStream = req.GetRequestStream())
                    reqStream.Write(reqData, 0, reqData.Length);

                using (WebResponse res = req.GetResponse())
                using (Stream resSteam = res.GetResponseStream())
                using (StreamReader sr = new StreamReader(resSteam))
                {
                    string xmlData = sr.ReadToEnd();
                    Read(xmlData);
                }
            }
            catch(Exception ex)
            {
                Logging.LogMessageToFile("Error trying to grab a quote: " + ex.Message, 2);                               
            }
        }

        //=================================================================================
        // Set the quote and author variables
        //=================================================================================
        private void Read(string xmlData)
        {
            XDocument doc = XDocument.Parse(xmlData);
            XElement quote = doc.Root.Element("quote");
            authQuote = quote.Element("quoteText").Value;
            quoteAuth = "- " + quote.Element("quoteAuthor").Value;
            Logging.LogMessageToFile("Found a quote: \"" + authQuote + " - " + quoteAuth, 0);
            return;
        }

        //=================================================================================                                                                                     
        // Add the quote, author and a background box to the wallpaper
        //=================================================================================
        private void addText(string wallpaper)
        {
                
            string wp = wallpaper;
            Bitmap bitmap = (Bitmap)Image.FromFile(wallpaper);
            int w = bitmap.Width;
            int h = bitmap.Height;
            int x = 100;
            int y = 100;


            Font quoteFont = new Font("Segio UI", 34, FontStyle.Italic);

            SizeF size = TextRenderer.MeasureText(quoteAuth, quoteFont);

            Graphics graphics = Graphics.FromImage(bitmap);

            {
                // Paint a semi transparate background box for the quote to reside in
                Color customColor = Color.FromArgb(70, Color.Gray);
                SolidBrush shadowBrush = new SolidBrush(customColor);
                graphics.FillRectangle(shadowBrush, x, y, size.Width, size.Height);

                {
                    // Add the quote
                    PointF firstLocation = new PointF(x, y);
                    PointF secondLocation = new PointF(75, 100);
                    graphics.DrawString(quoteAuth, quoteFont, Brushes.White, firstLocation);
                    // graphics.DrawString(auth, quoteFont, Brushes.White, secondLocation);
                }

            }
            // Save a copy
            bitmap.Save("wallpaper.jpg");
        }
    }
}
