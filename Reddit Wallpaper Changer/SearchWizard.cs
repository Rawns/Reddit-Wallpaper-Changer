using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reddit_Wallpaper_Changer
{
    public partial class SearchWizard : Form
    {
        private RWC form1;

        public SearchWizard(RWC form1)
        {
            InitializeComponent();
            this.form1 = form1;
        }

        private void SearchWizard_Load(object sender, EventArgs e)
        {
            searchQuery.Text = form1.searchQueryValue;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("http://www.reddit.com/wiki/search");
        }

        private void searchQuery_TextChanged(object sender, EventArgs e)
        {
            form1.changeSearchQuery(searchQuery.Text);
        }

        private void btnNSFWFilter_Click(object sender, EventArgs e)
        {
            if (searchQuery.Text.Contains("nsfw:yes"))
            {
                searchQuery.Text = searchQuery.Text.Replace("nsfw:yes", "nsfw:no");
            }
            else if (searchQuery.Text.Contains("nsfw:no"))
            {
                searchQuery.Text = searchQuery.Text.Replace("nsfw:no", "nsfw:yes");
            }
            else
            {
                searchQuery.Text = searchQuery.Text + " nsfw:no";
            }

        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            Properties.Settings.Default.searchQuery = searchQuery.Text;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
