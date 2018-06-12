﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PocetniZaslon.MDI_Forme
{
	public partial class FrmPregledTransakcija : Form
	{
		UpravljanjeBankovnimRacunima radnjaNadBankovnimRacunom = new UpravljanjeBankovnimRacunima();
		UpravljanjeTransakcijom radnjaNadTransakcijom = new UpravljanjeTransakcijom();

		BindingList<Bankovni_racun> listaBankovnihRacuna = null;
		BindingList<Vrsta_racuna> listaVrstaRacuna = null;
		BindingList<Vrsta_transakcije> listaVrstaTransakcije = null;
		BindingList<Transakcija> listaTransakcija = null;
		BindingList<Transakcija_investicije> listaTransakcijaInvesticije = null;
		BindingList<Kategorije_transakcije> listaKategorijaTransakcija = null;
		Korisnik trenutniKorisnik = null;

		BindingList<Bankovni_racun> listaOznacenihBankovnihRacuna = null;
		BindingList<Kategorije_transakcije> listaOznacenihKategorija = null;


		DateTime? vrijemeOd = null;
		DateTime? vrijemeDo = null;
		int vrstaTransakcije = 0;

		BindingList<PrikazTransakcije> listaPrikazaTransakcija = new BindingList<PrikazTransakcije>();

		//Konstruktor.
		public FrmPregledTransakcija(Korisnik korisnik)
		{
			trenutniKorisnik = korisnik;
			InitializeComponent();
		}

		private void FrmPregledTransakcija_Load(object sender, EventArgs e)
		{
			PrilagodiIzgledForme();

			dtpVrijemeOd.Value = DateTime.Now;
			dtpVrijemeDo.Value = DateTime.Now.AddMonths(1);

			DohvatiSveKorisnikoveZapise();
		}



		/// <summary>
		/// Dohvaćanje svih korisnikovih bankovnih računa, transakcija, kategorija i transakcija investicije.
		/// Dohvaćaju se još i vrste transakcija.
		/// </summary>
		private void DohvatiSveKorisnikoveZapise()
		{
			//Dohvaćanje i prikazivanje svih korisnikovih bankovnih računa (uključujući vrstu računa).
			listaBankovnihRacuna = new BindingList<Bankovni_racun>();
			listaBankovnihRacuna = radnjaNadBankovnimRacunom.PrikaziBankovneRacunePremaKorisniku(trenutniKorisnik);
			listaVrstaRacuna = new BindingList<Vrsta_racuna>();
			listaVrstaRacuna = radnjaNadBankovnimRacunom.PrikaziVrsteBankovnihRacuna();

			//Dohvaćanje vrsta transakcija.
			listaVrstaTransakcije = new BindingList<Vrsta_transakcije>();
			listaVrstaTransakcije = radnjaNadTransakcijom.DohvatiVrsteTransakcija();

			//Dohvaćanje Transakcija i transakcija investicija.
			listaTransakcija = new BindingList<Transakcija>();
			listaTransakcija = radnjaNadTransakcijom.DohvatiSveTransakcije(listaBankovnihRacuna);
			listaTransakcijaInvesticije = new BindingList<Transakcija_investicije>();
			listaTransakcijaInvesticije = radnjaNadTransakcijom.DohvatiSveTransakcijeInvesticija(listaBankovnihRacuna);

			//Dohvaćanje svih korisnikovih kategorije
			OsvjeziKategorije();


			if (listaBankovnihRacuna == null)
			{
				MessageBox.Show("Ne postoje bankovni računi!");
				return;
			}
			else btnOsvjeziTransakcije.Enabled = true;

			using (var db = new WalletEntities())
			{
				//Sve transakcije spremamo u listu prikaza transakcije
				foreach (Transakcija transakcija in listaTransakcija)
				{
					if (transakcija != null)
					{
						db.Transakcija.Attach(transakcija);

						BindingList<Kategorije_transakcije> listakategorijaTransakcije = new BindingList<Kategorije_transakcije>(transakcija.Kategorije_transakcije.ToList());
						Kategorije_transakcije kategorija = null;
						foreach (Kategorije_transakcije kategorijaTransakcije in listakategorijaTransakcije)
						{
							kategorija = kategorijaTransakcije;
						}

						db.Kategorije_transakcije.Attach(kategorija);

						PrikazTransakcije noviPrikazTransakcije = new PrikazTransakcije(
							transakcija.vrijeme_transakcije.Value,
							transakcija.Bankovni_racun,
							transakcija.iban,
							transakcija.iznos_transakcije,
							listakategorijaTransakcije,
							transakcija.opis_transakcije,
							kategorija.id_vrsta_transakcije
							);

						db.Entry(transakcija).State = System.Data.Entity.EntityState.Detached;
						db.Entry(kategorija).State = System.Data.Entity.EntityState.Detached;
						listaPrikazaTransakcija.Add(noviPrikazTransakcije);
					}
				}
				//Sve transakcije investicije spremamo u listu prikaza transakcije
				foreach (Transakcija_investicije transakcija in listaTransakcijaInvesticije)
				{
					if (transakcija != null)
					{
						db.Transakcija_investicije.Attach(transakcija);
						Investicija investicija = transakcija.Investicija;
						db.Investicija.Attach(investicija);

						PrikazTransakcije noviPrikazTransakcije = new PrikazTransakcije(
							transakcija.vrijeme_transakcije_investicije.Value,
							transakcija.Bankovni_racun,
							transakcija.iban,
							transakcija.iznos_transakcije_investicije.Value,
							transakcija.kolicina_investicije,
							investicija.naziv_investicije,
							transakcija.id_vrsta_transakcije
							);
						db.Entry(transakcija).State = System.Data.Entity.EntityState.Detached;
						listaPrikazaTransakcija.Add(noviPrikazTransakcije);
					}
				}
			}

			//Vezemo sve bankovne racune, vrste racuna, vrste transakcija na binding source-ove
			bindingSourceBankovniRacuni.DataSource = listaBankovnihRacuna;
			BindingSourceVrstaRacuna.DataSource = listaVrstaRacuna;
			bindingSourceVrstaTransakcije.DataSource = listaVrstaTransakcije;
		}

		
		/// <summary>
		/// Filtriranje svih korisnikovih transakcija prema označenim check boxevima i odabranom vremenskom razdoblju.
		/// </summary>
		private void OsvjeziPrikazTransakcija()
		{
			//Dohvaćanje vremena sa forme.
			vrijemeOd = null;
			vrijemeDo = null;
			if (!chkVrijeme.Checked)
			{
				vrijemeOd = dtpVrijemeOd.Value;
				vrijemeDo = dtpVrijemeDo.Value;
			}

			//Dohvaćanje vrste transakcije: prihod(1) ili rashod(2) ili i prihodi i rashodi(0)
			if (chkObicneTransakcije.Checked == true && chkTransakcijeInvesticija.Checked == true) vrstaTransakcije = 0;
			else if (chkObicneTransakcije.Checked == true && chkTransakcijeInvesticija.Checked == false) vrstaTransakcije = 1;
			else if (chkObicneTransakcije.Checked == false && chkTransakcijeInvesticija.Checked == true) vrstaTransakcije = 2;
			else MessageBox.Show("Nisu označeni ni prihodi ni rashodi");

			//Dohvaćamo označene bankovne račune iz dataGridView-a bankovnih računa.
			listaOznacenihBankovnihRacuna = new BindingList<Bankovni_racun>();
			foreach (DataGridViewRow red in dgvBankovniRacuni.Rows)
			{
				if (red.Cells[1].Value.ToString() == "True" || red.Cells[1].Value.ToString() == "true")
				{
					listaOznacenihBankovnihRacuna.Add(red.DataBoundItem as Bankovni_racun);
				}
			}

			//Dohvaćamo označene kategorije iz dataGridView-a kategorija.
			listaOznacenihKategorija = new BindingList<Kategorije_transakcije>();
			foreach (DataGridViewRow red in dgvKategorije.Rows)
			{
				if (red.Cells[1].Value.ToString() == "True" || red.Cells[1].Value.ToString() == "true")
				{
					listaOznacenihKategorija.Add(red.DataBoundItem as Kategorije_transakcije);
				}
			}

			//Stvaranje liste prikaza transakcija koji će se prikazivati u datagridviewu
			BindingList<PrikazTransakcije> listaFiltriranihPrikazaTransakcije = new BindingList<PrikazTransakcije>();


			//Filtriramo popis Prikaza Transakcija prema uvjetima forme
			bool uvjet;
			foreach (PrikazTransakcije prikazTransakcije in listaPrikazaTransakcija)
			{
				//Provjera vremena
				if (vrijemeOd != null) if (vrijemeOd > prikazTransakcije.Datum || prikazTransakcije.Datum > vrijemeDo) continue;

				//Provjera prihoda(1) ili rashoda(2) ili i prihodi i rashodi(0)


				//provjera bankovnih računa
				uvjet = false;
				if (listaOznacenihBankovnihRacuna == null) MessageBox.Show("Prazna lista racuna!");

				using (var db = new WalletEntities())
				{
					foreach (Bankovni_racun racun in listaOznacenihBankovnihRacuna)
					{
						db.Bankovni_racun.Attach(racun);

						if (prikazTransakcije.iban == racun.iban)
						{
							//Zadovoljava uvjet da transakcija ima jedan od označenih računa
							uvjet = true;
						}
						db.Entry(racun).State = System.Data.Entity.EntityState.Detached;
						if (uvjet == true) break;
					}
				}
				if (uvjet == false) continue;

				//Provjera kategorija
				uvjet = false;
				if (prikazTransakcije.KategorijeTransakcije != null)
				{
					using (var db = new WalletEntities())
					{
						foreach (Kategorije_transakcije kategorija in prikazTransakcije.KategorijeTransakcije)
						{
							foreach (Kategorije_transakcije oznacenaKategorija in listaOznacenihKategorija)
							{
								db.Kategorije_transakcije.Attach(kategorija);
								int idKategorijePrikaza = kategorija.id_kategorije_transakcije;
								db.Entry(kategorija).State = System.Data.Entity.EntityState.Detached;
								db.Kategorije_transakcije.Attach(oznacenaKategorija);

								if (idKategorijePrikaza == oznacenaKategorija.id_kategorije_transakcije)
								{
									//Zadovoljava uvjet da transakcija ima jednu od označenih kategorija
									uvjet = true;
								}
								db.Entry(oznacenaKategorija).State = System.Data.Entity.EntityState.Detached;
								if (uvjet == true) break;
							}
							if (uvjet == true) break;
						}
					}
				}
				if (uvjet == false) continue;

				listaFiltriranihPrikazaTransakcije.Add(prikazTransakcije);
			}

			listaFiltriranihPrikazaTransakcije.OrderByDescending(x => x.Datum);
			bindingSourcePregledTransakcija.Clear();
			bindingSourcePregledTransakcija.DataSource = listaFiltriranihPrikazaTransakcije;
		}
		


		#region CheckBoxevi funkcionalnosti

		//Obicne transakcije check box.
		private void chkObicneTransakcije_CheckedChanged(object sender, EventArgs e)
		{
			PrilagodiIzgledForme();
		}

		//Transakcije investicija check box.
		private void chkTransakcijeInvesticija_CheckedChanged(object sender, EventArgs e)
		{

		}

		//Prihodi check box.
		private void chkPrihodi_CheckedChanged(object sender, EventArgs e)
		{
			OsvjeziKategorije();
		}

		//Rashodi check box.
		private void chkRashodi_CheckedChanged(object sender, EventArgs e)
		{
			OsvjeziKategorije();
		}

		//Svo vrijeme check box.
		private void chkVrijeme_CheckedChanged(object sender, EventArgs e)
		{
			dtpVrijemeOd.Enabled = !(chkVrijeme.Checked);
			dtpVrijemeDo.Enabled = !(chkVrijeme.Checked);
		}

		//Svi računi check box.
		private void chkSviBankovniRacuni_CheckedChanged(object sender, EventArgs e)
		{
			OznacitiSveCheckBoxeve(dgvBankovniRacuni, chkSviBankovniRacuni);
		}

		//Sve kategorije check box.
		private void chkSveKategorije_CheckedChanged(object sender, EventArgs e)
		{
			OznacitiSveCheckBoxeve(dgvKategorije, chkSveKategorije);
		}

		//Promjena sadržaja dgvBankovniRacuni oznacuje sve bankovne racune.
		private void dgvBankovniRacuni_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			chkSviBankovniRacuni.Checked = true;
			OznacitiSveCheckBoxeve(dgvBankovniRacuni, chkSviBankovniRacuni);
		}
		private void dgvBankovniRacuni_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			chkSviBankovniRacuni.Checked = true;
			OznacitiSveCheckBoxeve(dgvBankovniRacuni, chkSviBankovniRacuni);
		}

		//Promjena sadržaja dgvKategorije oznacuje sve kategorije transakcija.
		private void dgvKategorije_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
		{
			chkSveKategorije.Checked = true;
			OznacitiSveCheckBoxeve(dgvKategorije, chkSveKategorije);
		}
		private void dgvKategorije_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
		{
			chkSveKategorije.Checked = true;
			OznacitiSveCheckBoxeve(dgvKategorije, chkSveKategorije);
		}

		/// <summary>
		/// Označuju se ili odznačuju se svi checkboxevi unutar DataGridView-a ovisno o nekoj drugoj checkBox oznaci.
		/// Unutar DataGridViewa stupac s checkboxevima mora biti na prvom mjestu.
		/// </summary>l
		private void OznacitiSveCheckBoxeve(DataGridView dataGrid, CheckBox oznaka)
		{
			foreach (DataGridViewRow red in dataGrid.Rows)
			{
				DataGridViewCheckBoxCell checkBox = (DataGridViewCheckBoxCell)red.Cells[1];
				checkBox.Value = oznaka.Checked;
			}
		}

		#endregion


		private void OsvjeziKategorije()
		{
			if (chkPrihodi.Checked == true && chkRashodi.Checked == true) listaKategorijaTransakcija = radnjaNadTransakcijom.DohvatiKategorijeKorisnika(trenutniKorisnik, 0);
			else if (chkPrihodi.Checked == true && chkRashodi.Checked == false) listaKategorijaTransakcija = radnjaNadTransakcijom.DohvatiKategorijeKorisnika(trenutniKorisnik, 1);
			else if (chkPrihodi.Checked == false && chkRashodi.Checked == true) listaKategorijaTransakcija = radnjaNadTransakcijom.DohvatiKategorijeKorisnika(trenutniKorisnik, 2);
			else if (chkPrihodi.Checked == false && chkRashodi.Checked == false) listaKategorijaTransakcija.Clear();
			bindingSourceKategorije.DataSource = listaKategorijaTransakcija;
		}

		private void PrilagodiIzgledForme()
		{
			lblPregledTransakcija.Location = new Point(this.Width / 2 - lblPregledTransakcija.Width / 2, lblPregledTransakcija.Location.Y);
			if (chkObicneTransakcije.Checked == false)
			{
				lblKategorije.Visible = false;
				dgvKategorije.Visible = false;
				chkSveKategorije.Visible = false;
				dgvBankovniRacuni.Height = dgvKategorije.Location.Y + dgvKategorije.Height - dgvBankovniRacuni.Location.Y;
			}
			else
			{
				lblKategorije.Visible = true;
				dgvKategorije.Visible = true;
				chkSveKategorije.Visible = true;
				dgvBankovniRacuni.Height = lblKategorije.Location.Y - dgvBankovniRacuni.Location.Y - 20;
			}
		}

		private void btnOsvjeziTransakcije_Click(object sender, EventArgs e)
		{
			OsvjeziPrikazTransakcija();
		}


	}
}
