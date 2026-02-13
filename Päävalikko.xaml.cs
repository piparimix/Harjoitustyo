using System.Windows;

namespace Harjoitustyö
{
    /// <summary>
    /// Interaction logic for Päävalikko.xaml
    /// </summary>
    public partial class Päävalikko : Window
    {
        public Päävalikko()
        {
            InitializeComponent();
            Tietokanta.AlustaTietokanta();
        }

        private void Muokkaa_Lasku_Click(object sender, RoutedEventArgs e)
        {
            Muokkaa_Lasku myWindow = new Muokkaa_Lasku();
            myWindow.WindowState = this.WindowState;
            myWindow.Show();
            this.Close();
        }

        private void Uusi_Lasku_Click(object sender, RoutedEventArgs e)
        {

            Uusi_Lasku myWindow = new Uusi_Lasku();
            myWindow.WindowState = this.WindowState;
            myWindow.Show();
           
            this.Close();
        }

        private void Tuotteet_Click(object sender, RoutedEventArgs e)
        {
            Tuote_Lista myWindow = new Tuote_Lista();
            myWindow.WindowState = this.WindowState;
            myWindow.Show();
            this.Close();
        }

        private void Kaikki_Lasku_Click(object sender, RoutedEventArgs e)
        {
            Kaikki_Laskut myWindow = new Kaikki_Laskut();
            myWindow.WindowState = this.WindowState;
            myWindow.Show();
            this.Close();
        }

        private void Sulje_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
