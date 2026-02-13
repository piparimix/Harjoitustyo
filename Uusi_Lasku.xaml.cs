using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Harjoitustyö;

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
            // Alustetaan tuotelista hakemalla tuotteet tietokannasta
            VarastoTuotteet = Tietokanta.HaeKaikkiTuotteet();

            // Alustetaan komponentit ja asetetaan laskun numero sekä viivakoodi
            InitializeComponent();
            try
            {
                
                int seuraavaNumero = Tietokanta.HaeSeuraavaLaskunNumero();
                Newlasku.LaskunNumero = seuraavaNumero;

                // Nyt BarcodeImage on olemassa ja sille voidaan asettaa lähde
                if (BarcodeImage != null)
                {
                    BarcodeImage.Source = Barcode.GenerateBarcode(seuraavaNumero.ToString());
                }

                this.DataContext = Newlasku;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Virhe alustuksessa: {ex.Message}");
            }
        }

        // Tallenna-painikkeen klikkaus tapahtuma, kutsuu Tietokanta-luokan tallennusmetodia ja palaa päävalikkoon onnistumisen jälkeen 
        private void Tallenna_Click(object sender, RoutedEventArgs e)
        {
            if (!OnkoTiedotKelvolliset()) return;

            // Käytetään Tietokanta-luokan tallennusta
            bool onnistui = Tietokanta.TallennaLasku(Newlasku);

            if (onnistui)
            {
                MessageBox.Show("Lasku tallennettu onnistuneesti!");

                // Palataan päävalikkoon
                Päävalikko MyWindow = new Päävalikko();
                MyWindow.WindowState = this.WindowState;
                MyWindow.Show();
                this.Close();
            }
        }

        // "Tallenna ja luo PDF" -painikkeen klikkaus tapahtuma, joka luo PDF:n, avaa sen ja tallentaa laskun tietokantaan ja palaa päävalikkoon onnistumisen jälkeen
        private void ToPDF_AND_Save_Click(object sender, RoutedEventArgs e)
        {
            if (!OnkoTiedotKelvolliset()) return;

            try
            {
                // Määritellään polku Laskut-kansioon, joka sijaitsee projektin juurihakemistossa
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

                // Tallennetaan tietokantaan
                bool onnistui = Tietokanta.TallennaLasku(Newlasku);

                if (onnistui)
                {
                    MessageBox.Show("PDF luotu ja tiedot tallennettu tietokantaan.");
                    Päävalikko MyWindow = new Päävalikko();
                    MyWindow.WindowState = this.WindowState;
                    MyWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Virhe: {ex.Message}");
            }
        }

        // Metodi, joka tarkistaa, että kaikki tarvittavat tiedot on syötetty ennen tallennusta tai PDF:n luontia. kutsutaan Tallenna_Click ja ToPDF_AND_Save_Click tapahtumissa.
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

        // Peruuta painikkeen klikkaus tapahtuma, joka palaa päävalikkoon ilman tallennusta
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Päävalikko MyWindow = new Päävalikko();
            MyWindow.WindowState = this.WindowState;
            MyWindow.Show();
            this.Close();
        }

        // Metodi, joka päivittää laskun summan näytölle. Kutsutaan DataGridin RowEditEnding-tapahtumassa, jotta summa päivittyy aina, kun tuoteriviä muokataan.
        public void PäivitäSumma()
        {
            if (Newlasku != null && Newlasku.Tuotteet != null)
            {
                // Lasketaan summa käyttämällä Models.cs:n logiikkaa
                Total.Text = $"{Newlasku.Yhteensä:C2}";
            }
        }

        // DataGridin RowEditEnding-tapahtuma, joka kutsuu PäivitäSumma-metodia, jotta summa päivittyy aina, kun tuoteriviä muokataan.
        private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            // Päivitetään summa viiveellä, jotta uudet arvot ehtivät mukaan
            Dispatcher.BeginInvoke(new Action(() => { PäivitäSumma(); }));
        }
    }
}