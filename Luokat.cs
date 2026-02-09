using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq; // Tarvitaan LINQ-kyselyihin (FirstOrDefault)

namespace Harjoitustyö
{
    // --- TUOTE (Varastotuote) ---
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

        private float _alv = 24;
        public float ALV
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

    // --- LASKU ---
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

        // Laskulla on lista Laskurivejä
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

    // --- LASKURIVI (Laskun rivi) ---
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

        private float _alv = 24;
        public float ALV { get { return _alv; } set { _alv = value; OnPropertyChanged("ALV"); } }

        public decimal ALV_Euro { get { return (A_Hinta * ((decimal)ALV / 100m)) * Määrä; } }
        public decimal Yhteensä { get { return (Määrä * A_Hinta) + ALV_Euro; } }

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

    // --- APULUOKAT ---
    public class Laskuttaja
    {
        public string Nimi { get; set; } = "Rakennus OY";
        public string Osoite { get; set; } = "Rakennustie 15";
        public string Postinumero { get; set; } = "00100 Helsinki";
    }

    public class Asiakas : IDataErrorInfo
    {
        public string Nimi { get; set; }
        public string Osoite { get; set; }
        public string Postinumero { get; set; }
        public string Lisätiedot { get; set; } = "";

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Nimi" && string.IsNullOrWhiteSpace(Nimi)) return "Pakollinen";
                if (columnName == "Osoite" && string.IsNullOrWhiteSpace(Osoite)) return "Pakollinen";
                return null;
            }
        }
        public string Error { get { return null; } }
    }
}