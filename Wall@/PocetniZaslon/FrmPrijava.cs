﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PocetniZaslon
{
    public partial class FrmPrijava : Form
    {
        public FrmPrijava()
        {
            InitializeComponent();

        }

        private void btnPrijava_Click(object sender, EventArgs e)
        {
            korisnik trenutniKorisnik = new korisnik(); // Instanciranje korisnika koje pri uspješnoj prijavi bilježi točno tog korisnika.

            /*
             Čitanje liste korisnika, tako da možemo provjeriti točnost login podataka.
             Vidi skriptu.
             */
            BindingList<korisnik> korisnici = null;
            using (var db = new Entities())
            {
                korisnici = new BindingList<korisnik>(db.korisnik.ToList());
            }

            bool provjera = false; // Varijabla za provjeru korisnika (da ne ispisuje foreach više puta poruke).
            foreach (var item in korisnici)
            {
                if (txtEmail.Text == item.email)
                {
                    provjera = true;
                    trenutniKorisnik = item;
                }
                else
                {
                    provjera = false;
                }
            }

            if (provjera == true)
            {
                if (txtLozinka.Text == trenutniKorisnik.lozinka)
                {
                    MessageBox.Show("Prijava uspješna");
                    FrmGlavniIzbornik glavniIzbornik = new FrmGlavniIzbornik(); // Ako je login dobar onda se pokrene FrmGlavniIzbornik, login je skriven i zatvara se skupa sa izbornikom.
                    this.Hide();
                    glavniIzbornik.ShowDialog();
                    this.Close();
                }
                else MessageBox.Show("Kriva lozinka");
            }
        }

        private void cboxMaskLozinka_CheckedChanged(object sender, EventArgs e) // Checkbox za otkrivanje/skrivanje lozinke.
        {
            if (cboxMaskLozinka.Checked) txtLozinka.UseSystemPasswordChar = false;

            else txtLozinka.UseSystemPasswordChar = true;

        }
    }
}
