using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Harjoitustyö
{
    // Tuote-luokka, joka sisältää tuotteen tiedot ja toteuttaa INotifyPropertyChanged-rajapinnan.
    // Tuotteella on ID, Nimi, Määrä, Yksikkö, A_Hinta ja ALV.
    // INotifyPropertyChanged toteutus ilmoittaa, kun ominaisuuksien arvot muuttuvat, jotta UI päivittyy automaattisesti.
    public class Tuote : INotifyPropertyChanged
    {
        public int Tuote_ID { get; set; }

        private string _nimi;
        public string Nimi
        {
            get { return _nimi; }
            set { _nimi = value; OnPropertyChanged("Nimi"); }
        }

        private int _määrä;
        public int Määrä
        {
            get { return _määrä; }
            set { _määrä = value; OnPropertyChanged("Määrä"); }
        }

        private string _yksikkö;
        public string Yksikkö
        {
            get { return _yksikkö; }
            set { _yksikkö = value; OnPropertyChanged("Yksikkö"); }
        }

        private decimal _a_hinta;
        public decimal A_Hinta
        {
            get { return _a_hinta; }
            set { _a_hinta = value; OnPropertyChanged("A_Hinta"); }
        }

        private decimal _alv = 25.5m;
        public decimal ALV
        {
            get { return _alv; }
            set { _alv = value; OnPropertyChanged("ALV"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    // Lasku-luokka, joka sisältää laskun tiedot ja toteuttaa INotifyPropertyChanged-rajapinnan.
    // Laskulla on Päiväys, Eräpäivä, LaskunNumero, lista Laskurivejä, LaskuttajaInfo ja AsiakasInfo.
    // Laskulla on myös Yhteensä- ja ALV_Euro_Yhteensä-laskukaavat, jotka laskevat laskun kokonaissumman ja ALV:n euroina.
    // INotifyPropertyChanged toteutus ilmoittaa, kun ominaisuuksien arvot muuttuvat, jotta UI päivittyy automaattisesti.
    public class Lasku : INotifyPropertyChanged
    {
        public DateTime Päiväys { get; set; } = DateTime.Now;
        public DateTime Eräpäivä { get; set; } = DateTime.Now.AddDays(28);

        private int _laskunNumero;
        public int LaskunNumero
        {
            get { return _laskunNumero; }
            set { _laskunNumero = value; OnPropertyChanged("LaskunNumero"); }
        }

        public ObservableCollection<Laskurivi> Tuotteet { get; set; } = new ObservableCollection<Laskurivi>();

        public Laskuttaja LaskuttajaInfo { get; set; } = new Laskuttaja();
        public Asiakas AsiakasInfo { get; set; } = new Asiakas();

        public decimal Yhteensä
        {
            get
            {
                decimal summa = 0;
                foreach (var rivi in Tuotteet) summa += rivi.Yhteensä;
                return summa;
            }
        }

        public decimal ALV_Euro_Yhteensä
        {
            get
            {
                decimal summa = 0;
                foreach (var rivi in Tuotteet) summa += rivi.ALV_Euro;
                return summa;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public string PDFPolku
        {
            get
            {
                return $"Lataa Lasku_{LaskunNumero}.pdf";
            }
        }
    }

    // Laskurivi-luokka, joka sisältää laskurivin tiedot ja toteuttaa INotifyPropertyChanged- ja IDataErrorInfo-rajapinnat.
    // Laskurivi sisältää Tuote_ID:n, Nimen, Määrän, Yksikön, A_Hinnan, ALV:n, ALV_Euron ja Yhteensä-laskukaavat.
    // Kun Tuote_ID asetetaan, haetaan siihen liittyvät tiedot varastotuotteista ja päivitetään Nimi, Yksikkö, A_Hinta ja ALV.
    // Kun Määrä tai A_Hinta muuttuu, päivitetään Yhteensä ja ALV_Euro.
    public class Laskurivi : INotifyPropertyChanged, IDataErrorInfo
    {
        public int Laskurivi_ID { get; set; }

        private int _tuoteId;
        public int Tuote_ID
        {
            get { return _tuoteId; }
            set
            {
                _tuoteId = value;
                OnPropertyChanged("Tuote_ID");
                HaeTuotetiedot(); // Kun ID muuttuu, haetaan tiedot varastosta
            }
        }

        // Kun Tuote_ID asetetaan, haetaan siihen liittyvät tiedot varastotuotteista
        private void HaeTuotetiedot()
        {
            // Haetaan tiedot Uusi_Lasku -ikkunan staattisesta varastolistasta
            if (Uusi_Lasku.VarastoTuotteet != null)
            {
                Tuote loytynyt = null;
                foreach (var t in Uusi_Lasku.VarastoTuotteet)
                {
                    if (t.Tuote_ID == this.Tuote_ID)
                    {
                        loytynyt = t;
                        break;
                    }
                }

                if (loytynyt != null)
                {
                    this.Nimi = loytynyt.Nimi;
                    this.Yksikkö = loytynyt.Yksikkö;
                    this.A_Hinta = loytynyt.A_Hinta;
                    this.ALV = loytynyt.ALV;
                    if (this.Määrä == 0) this.Määrä = 1;
                }
            }
        }

        // Nimi on varastotuotteista haettava tieto, joka asetetaan Tuote_ID:n perusteella. Kun Nimi muuttuu, päivitetään UI.
        private string _nimi;
        public string Nimi { get { return _nimi; } set { _nimi = value; OnPropertyChanged("Nimi"); } }

        private int _määrä;
        public int Määrä
        {
            get { return _määrä; }
            set { _määrä = value; OnPropertyChanged("Määrä"); OnPropertyChanged("Yhteensä"); OnPropertyChanged("ALV_Euro"); }
        }

        private string _yksikkö;
        public string Yksikkö { get { return _yksikkö; } set { _yksikkö = value; OnPropertyChanged("Yksikkö"); } }

        private decimal _a_hinta;
        public decimal A_Hinta
        {
            get { return _a_hinta; }
            set { _a_hinta = value; OnPropertyChanged("A_Hinta"); OnPropertyChanged("Yhteensä"); OnPropertyChanged("ALV_Euro"); }
        }

        // Oletetaan, että ALV on sama kaikille tuotteille.
        // mutta Sitä voidaan muuttaa tuote listassa jokaisen tuotteen kohdalla, mutta oletuksena se on 25.5%.
        private decimal _alv = 25.5m;
        public decimal ALV { get { return _alv; } set { _alv = value; OnPropertyChanged("ALV"); } }

        public decimal ALV_Euro { get { return (A_Hinta * (ALV / 100m)) * Määrä; } }
        public decimal Yhteensä { get { return (Määrä * A_Hinta) + ALV_Euro; } }


        // IDataErrorInfo toteutus, joka tarkistaa, että Nimi ei ole tyhjä ja Määrä on suurempi kuin 0. Jos jokin näistä ehdoista ei täyty, palautetaan virheilmoitus.
        public string this[string columnName]
        {
            get
            {
                if (columnName == "Nimi" && string.IsNullOrWhiteSpace(Nimi)) return "Pakollinen";
                if (columnName == "Määrä" && Määrä <= 0) return ">0";
                return null;
            }
        }

        public string Error { get { return null; } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    // Laskuttaja-luokka, joka sisältää laskuttajan tiedot. Oletuksena nimi on "Rakennus OY", osoite "Rakennustie 15" ja postinumero "00100 Helsinki".
    public class Laskuttaja
    {
        public string Nimi { get; set; } = "Rakennus OY";
        public string Osoite { get; set; } = "Rakennustie 15";
        public string Postinumero { get; set; } = "00100 Helsinki";
    }

    // Asiakas-luokka, joka sisältää asiakkaan tiedot ja toteuttaa IDataErrorInfo-rajapinnan tietojen validointia varten. Validointi tarkistaa, että nimi ja osoite eivät ole tyhjiä.
    public class Asiakas : IDataErrorInfo
    {
        public string Nimi { get; set; }
        public string Osoite { get; set; }
        public string Postinumero { get; set; }
        public string Lisätiedot { get; set; } = "";

        // IDataErrorInfo toteutus, joka tarkistaa, että Nimi, Osoite ja Postinumero eivät ole tyhjiä. Jos jokin näistä on tyhjä, palautetaan virheilmoitus "Pakollinen".
        public string this[string columnName]
        {
            get
            {
                if (columnName == "Nimi" && string.IsNullOrWhiteSpace(Nimi)) return "Pakollinen";
                if (columnName == "Osoite" && string.IsNullOrWhiteSpace(Osoite)) return "Pakollinen";
                if (columnName == "Postinumero" && string.IsNullOrWhiteSpace(Postinumero)) return "Pakollinen";
                return null;
            }
        }
        public string Error { get { return null; } }
    }
}