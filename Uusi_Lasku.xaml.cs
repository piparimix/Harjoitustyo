using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Harjoitustyö; // Tärkeä: Ottaa käyttöön Luokat.cs ja Tietokanta.cs

namespace Harjoitustyö
{
    public partial class Uusi_Lasku : Window
    {
        // Tuotelista, jota Luokat.cs:n Laskurivi-luokka käyttää tietojen hakuun
        public static ObservableCollection<Tuote> VarastoTuotteet { get; set; }

        // Itse lasku-olio
        public Lasku Newlasku { get; set; } = new Lasku();

        public Uusi_Lasku()
        {
            InitializeComponent();

            try
            {
                // 1. Ladataan tuotteet tietokannasta alasvetovalikkoa varten
                VarastoTuotteet = Tietokanta.HaeKaikkiTuotteet();

                // 2. Haetaan seuraava vapaa laskunumero
                int seuraavaNumero = Tietokanta.HaeSeuraavaLaskunNumero();
                Newlasku.LaskunNumero = seuraavaNumero;

                // 3. Generoidaan viivakoodi (varmista että Barcode-luokka on olemassa)
                if (BarcodeImage != null)
                {
                    BarcodeImage.Source = Barcode.GenerateBarcode(seuraavaNumero.ToString());
                }

                // 4. Asetetaan DataContext, jotta XAML näkee tiedot
                this.DataContext = Newlasku;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Virhe alustuksessa: {ex.Message}");
            }
        }

        private void Tallenna_Click(object sender, RoutedEventArgs e)
        {
            if (!OnkoTiedotKelvolliset()) return;

            // Käytetään Tietokanta-luokan tallennusta
            bool onnistui = Tietokanta.TallennaLasku(Newlasku);

            if (onnistui)
            {
                MessageBox.Show("Lasku tallennettu onnistuneesti!");

                // Palataan päävalikkoon (varmista että Päävalikko-ikkuna on olemassa)
                Päävalikko MyWindow = new Päävalikko();
                MyWindow.Show();
                this.Close();
            }
        }

        private void ToPDF_AND_Save_Click(object sender, RoutedEventArgs e)
        {
            if (!OnkoTiedotKelvolliset()) return;

            try
            {
                // Määritellään polku Laskut-kansioon
                string projectRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\"));
                string folderPath = System.IO.Path.Combine(projectRoot, "Laskut");

                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string fileName = $"Lasku_{Newlasku.LaskunNumero}.pdf";
                string fullPath = System.IO.Path.Combine(folderPath, fileName);

                // Luodaan PDF
                PdfService.LuoPDF(Newlasku, fullPath);

                // Avataan PDF
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });

                // Tallennetaan kantaan
                bool onnistui = Tietokanta.TallennaLasku(Newlasku);

                if (onnistui)
                {
                    MessageBox.Show("PDF luotu ja tiedot tallennettu tietokantaan.");
                    Päävalikko MyWindow = new Päävalikko();
                    MyWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Virhe: {ex.Message}");
            }
        }

        private bool OnkoTiedotKelvolliset()
        {
            // Tarkistetaan asiakastiedot
            if (string.IsNullOrWhiteSpace(Newlasku.AsiakasInfo.Nimi) ||
                string.IsNullOrWhiteSpace(Newlasku.AsiakasInfo.Osoite) ||
                string.IsNullOrWhiteSpace(Newlasku.AsiakasInfo.Postinumero))
            {
                MessageBox.Show("Täytä asiakastiedot!", "Huomio", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Tarkistetaan onko rivejä
            if (Newlasku.Tuotteet.Count == 0)
            {
                MessageBox.Show("Laskulla on oltava vähintään yksi tuoterivi.", "Huomio");
                return false;
            }

            // Tarkistetaan rivien sisältö
            foreach (var rivi in Newlasku.Tuotteet)
            {
                if (string.IsNullOrEmpty(rivi.Nimi) || rivi.Määrä <= 0)
                {
                    MessageBox.Show("Tarkista tuoterivit: Nimi puuttuu tai määrä on virheellinen.", "Virhe", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }
            return true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Päävalikko MyWindow = new Päävalikko();
            MyWindow.Show();
            this.Close();
        }

        public void PäivitäSumma()
        {
            if (Newlasku != null && Newlasku.Tuotteet != null)
            {
                // Lasketaan summa käyttämällä Models.cs:n logiikkaa
                Total.Text = $"{Newlasku.Yhteensä:C2}";
            }
        }

        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // Päivitetään summa viiveellä, jotta uudet arvot ehtivät mukaan
            Dispatcher.BeginInvoke(new Action(() => { PäivitäSumma(); }));
        }
    }
}