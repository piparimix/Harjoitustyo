using System.Windows;
using System.Linq;
using static Harjoitustyö.Uusi_Lasku;
using static Harjoitustyö.Tietokanta;
using static Harjoitustyö.PdfService;
using static Harjoitustyö.Barcode;

namespace Harjoitustyö
{
    /// <summary>
    /// Interaction logic for Muokkaa_Lasku.xaml
    /// </summary>
    public partial class Muokkaa_Lasku : Window
    {
        public Muokkaa_Lasku()
        {
            InitializeComponent();
        }

        private void btnPeruuta_Click(object sender, RoutedEventArgs e)
        {
            Päävalikko myWindow = new Päävalikko();
            myWindow.Show();
            this.Close();
        }

        private void btnTallenna_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataContext is Lasku muokattuLasku)
            {
                // 2. Call the database update method
                bool onnistui = Tietokanta.PäivitäLasku(muokattuLasku);

                if (onnistui)
                {
                    MessageBox.Show("Muutokset tallennettu onnistuneesti!");
                }
            }
            else
            {
                MessageBox.Show("Ei tallennettavia tietoja. Hae ensin lasku.");
            }
        }

        private void btnHae_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(txtLaskunNumero.Text, out int id))
            {
                // 2. Haetaan laskun perustiedot tietokannasta
                Lasku haettuLasku = Tietokanta.HaeLasku(id);

                if (haettuLasku != null)
                {
                    // 3. Haetaan laskulle kuuluvat tuotteet (käytetään olemassa olevaa metodia)
                    haettuLasku.Tuotteet = Tietokanta.HaeTuotteetLaskulle(id);

                    // 4. Asetetaan ikkunan DataContext löydetyksi laskuksi.
                    BarcodeImage.Source = Barcode.GenerateBarcode(haettuLasku.LaskunNumero.ToString());
                    this.DataContext = haettuLasku;
                    PäivitäSumma();
                }
                else
                {
                    MessageBox.Show("Laskua ei löytynyt numerolla " + id);
                }
            }
            else
            {
                MessageBox.Show("Syötä kelvollinen laskun numero.");
            }
        }

        // 5. Päivitetään ikkunan summa, jos lasku ja tuotteet on haettu onnistuneesti.
        public void PäivitäSumma()
        {
            if (this.DataContext is Lasku nykyinenLasku && nykyinenLasku.Tuotteet != null)
            {           
                decimal summa = nykyinenLasku.Tuotteet.Sum(t => (t.Yhteensä));
                Total.Text = $"{summa:C2}";
            }
            else
            {
                Total.Text = "0,00 €";
            }
        }

        private void btnHaeNimella_Click(object sender, RoutedEventArgs e)
        {
            string nimi = txtAsiakasNimi.Text;

            if (!string.IsNullOrWhiteSpace(nimi))
            {
                // Kutsu tietokantafunktiota tässä
                Lasku haettuLasku = Tietokanta.HaeNimellä(nimi);

                if (haettuLasku != null)
                {
                    haettuLasku.Tuotteet = Tietokanta.HaeTuotteetLaskulle(haettuLasku.LaskunNumero);

                    // 4. Asetetaan ikkunan DataContext löydetyksi laskuksi.
                    BarcodeImage.Source = Barcode.GenerateBarcode(haettuLasku.LaskunNumero.ToString());
                    this.DataContext = haettuLasku;
                    PäivitäSumma();
                }
                else
                {
                    MessageBox.Show("Laskua ei löytynyt nimellä: " + nimi);
                }
            }
            else
            {
                MessageBox.Show("Syötä kelvollinen nimi");
            }
        }
    }
}
