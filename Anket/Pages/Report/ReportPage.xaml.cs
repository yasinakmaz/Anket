using Anket.Services;
using System.Diagnostics;
using System.Text;
using System.IO;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.Maui.Storage;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Core;

namespace Anket.Pages.Report;

public partial class ReportPage : ContentPage
{
	private ReportingService _reportingService;
	private DateTime _startDate;
	private DateTime _startTime;
	private DateTime _endDate;
	private DateTime _endTime;
	private readonly IFileSaver _fileSaver;
	
	public ReportPage(IFileSaver fileSaver)
	{
		InitializeComponent();
		
		_fileSaver = fileSaver;
		_reportingService = new ReportingService();
		
		// Varsayılan olarak bugünün tarihini ayarla
		var today = DateTime.Today;
		_startDate = today;
		_startTime = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0);
		_endDate = today;
		_endTime = new DateTime(today.Year, today.Month, today.Day, 23, 59, 59);
		
		// DatePicker ve TimePicker'ları ayarla
		StartDatePicker.Date = _startDate;
		StartTimePicker.Time = new TimeSpan(0, 0, 0);
		EndDatePicker.Date = _endDate;
		EndTimePicker.Time = new TimeSpan(23, 59, 59);
		
		// Butonlara tıklama olaylarını ekle
		BtnBugun.Clicked += (s, e) => SetDateRange(ReportDateRange.Today);
		BtnHafta.Clicked += (s, e) => SetDateRange(ReportDateRange.ThisWeek);
		BtnAy.Clicked += (s, e) => SetDateRange(ReportDateRange.ThisMonth);
		BtnYil.Clicked += (s, e) => SetDateRange(ReportDateRange.ThisYear);
		
		// Tarih Uygula butonuna tıklama olayı ekle
		BtnTarihUygula.Clicked += OnTarihUygulaClicked;
		
		// Excel Rapor Al butonuna tıklama olayı ekle
		BtnExcel.Clicked += BtnExcel_Clicked;
		
		// Sayı gösterme butonlarına yenileme olayı ekle
		BtnMutlu.Clicked += (s, e) => RefreshReportAsync();
		BtnNotr.Clicked += (s, e) => RefreshReportAsync();
		BtnMutsuz.Clicked += (s, e) => RefreshReportAsync();
		
		// Date ve Time seçicilere değişiklik olaylarını ekle (Otomatik yenilemesiz)
		StartDatePicker.DateSelected += OnDateTimeChanged;
		StartTimePicker.PropertyChanged += OnTimeChanged;
		EndDatePicker.DateSelected += OnDateTimeChanged;
		EndTimePicker.PropertyChanged += OnTimeChanged;
	}
	
	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await RefreshReportAsync();
	}
	
	private void SetDateRange(ReportDateRange range)
	{
		var (startDate, endDate) = ReportingService.GetDateRange(range);
		
		_startDate = startDate;
		_startTime = startDate;
		_endDate = endDate;
		_endTime = endDate;
		
		// UI kontrollerini güncelle
		StartDatePicker.Date = startDate;
		StartTimePicker.Time = new TimeSpan(startDate.Hour, startDate.Minute, startDate.Second);
		EndDatePicker.Date = endDate;
		EndTimePicker.Time = new TimeSpan(endDate.Hour, endDate.Minute, endDate.Second);
		
		// Raporu yenile
		RefreshReportAsync();
	}
	
	private async Task RefreshReportAsync()
	{
		try
		{
			// Gösterge ekranını göster
			await ShowLoadingIndicator(true);
			
			// Tarih ve saat birleştirme
			var startDateTime = new DateTime(
				_startDate.Year, _startDate.Month, _startDate.Day,
				_startTime.Hour, _startTime.Minute, _startTime.Second);
				
			var endDateTime = new DateTime(
				_endDate.Year, _endDate.Month, _endDate.Day,
				_endTime.Hour, _endTime.Minute, _endTime.Second);
				
			// Eğer bitiş tarihi başlangıç tarihinden önceyse, hata göster
			if (endDateTime < startDateTime)
			{
				await DisplayAlert("Hata", "Bitiş tarihi, başlangıç tarihinden önce olamaz.", "Tamam");
				return;
			}
				
			// Verileri getir
			var results = await _reportingService.GetReportDataAsync(startDateTime, endDateTime);
				
			// UI'ı güncelle
			BtnMutlu.Text = $"{results.MutluCount} Mutlu";
			BtnNotr.Text = $"{results.NotrCount} Kararsız";
			BtnMutsuz.Text = $"{results.UzgunCount} Mutsuz";
				
			// İstatistik detayları
			int totalVotes = results.MutluCount + results.NotrCount + results.UzgunCount;
			if (totalVotes > 0)
			{
				double mutluYuzde = (double)results.MutluCount / totalVotes * 100;
				double notrYuzde = (double)results.NotrCount / totalVotes * 100;
				double uzgunYuzde = (double)results.UzgunCount / totalVotes * 100;
				
				// Butonların bilgilerini güncelle
				BtnMutlu.Text = $"{results.MutluCount} Mutlu (%{mutluYuzde:F1})";
				BtnNotr.Text = $"{results.NotrCount} Kararsız (%{notrYuzde:F1})";
				BtnMutsuz.Text = $"{results.UzgunCount} Mutsuz (%{uzgunYuzde:F1})";
			}
				
			Debug.WriteLine($"Rapor güncellendi: Mutlu={results.MutluCount}, Nötr={results.NotrCount}, Üzgün={results.UzgunCount}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Rapor yenilenirken hata: {ex.Message}");
			await DisplayAlert("Hata", "Rapor verileri getirilirken bir hata oluştu.", "Tamam");
		}
		finally
		{
			// Gösterge ekranını gizle
			await ShowLoadingIndicator(false);
		}
	}
	
	private async Task ShowLoadingIndicator(bool isVisible)
	{
		LoadingIndicator.IsVisible = isVisible;
		LoadingIndicator.IsRunning = isVisible;
		await Task.CompletedTask;
	}
	
	private void OnTarihUygulaClicked(object sender, EventArgs e)
	{
		// Tarih uygula butonuna tıklandığında raporu yenile
		RefreshReportAsync();
	}
	
	private void OnDateTimeChanged(object sender, DateChangedEventArgs e)
	{
		if (sender == StartDatePicker)
		{
			_startDate = e.NewDate;
		}
		else if (sender == EndDatePicker)
		{
			_endDate = e.NewDate;
		}
		
		// Otomatik rapor yenileme kaldırıldı
	}
	
	private void OnTimeChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != "Time") return;
		
		if (sender == StartTimePicker)
		{
			_startTime = new DateTime(_startDate.Year, _startDate.Month, _startDate.Day, 
				StartTimePicker.Time.Hours, StartTimePicker.Time.Minutes, StartTimePicker.Time.Seconds);
		}
		else if (sender == EndTimePicker)
		{
			_endTime = new DateTime(_endDate.Year, _endDate.Month, _endDate.Day, 
				EndTimePicker.Time.Hours, EndTimePicker.Time.Minutes, EndTimePicker.Time.Seconds);
		}
		
		// Otomatik rapor yenileme kaldırıldı
	}
	
	private async void BtnExcel_Clicked(object sender, EventArgs e)
	{
		try
		{
			// Yükleme göstergesini göster
			await ShowLoadingIndicator(true);
			
			// Tarih ve saat birleştirme
			var startDateTime = new DateTime(
				_startDate.Year, _startDate.Month, _startDate.Day,
				_startTime.Hour, _startTime.Minute, _startTime.Second);
				
			var endDateTime = new DateTime(
				_endDate.Year, _endDate.Month, _endDate.Day,
				_endTime.Hour, _endTime.Minute, _endTime.Second);
				
			// Eğer bitiş tarihi başlangıç tarihinden önceyse, hata göster
			if (endDateTime < startDateTime)
			{
				await DisplayAlert("Hata", "Bitiş tarihi, başlangıç tarihinden önce olamaz.", "Tamam");
				return;
			}
			
			// Verileri getir
			var results = await _reportingService.GetReportDataAsync(startDateTime, endDateTime);
			
			if ((results.SqliteRecords.Count + results.FirebaseRecords.Count) == 0)
			{
				await DisplayAlert("Bilgi", "Seçilen tarih aralığında rapor edilecek veri bulunamadı.", "Tamam");
				return;
			}
			
			// Excel dosyasını oluştur
			string fileName = $"Anket_Rapor_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
			string tempFilePath = Path.Combine(FileSystem.CacheDirectory, fileName);
			
			// Excel dosyasını oluştur ve içeriğini doldur
			await Task.Run(() => CreateExcelFile(results, startDateTime, endDateTime, tempFilePath));
			
			// Dosya kaydetme işlemi
			try
			{
				var fileStream = new MemoryStream(File.ReadAllBytes(tempFilePath));
				
				// Kullanıcıya dosyayı kaydetme seçeneği sun
				var result = await _fileSaver.SaveAsync(fileName, fileStream, new CancellationTokenSource().Token);
				
				if (result.IsSuccessful)
				{
					// Dosya başarıyla kaydedildi
					await DisplayAlert("Başarılı", $"Excel raporu başarıyla kaydedildi: {result.FilePath}", "Tamam");
					Debug.WriteLine($"Excel raporu kaydedildi: {result.FilePath}");
				}
				else
				{
					// Kullanıcı kaydetme işlemini iptal etti veya bir hata oluştu
					Debug.WriteLine("Dosya kaydetme işlemi iptal edildi veya başarısız oldu");
				}
			}
			catch (Exception ex)
			{
				await DisplayAlert("Hata", $"Dosya kaydedilemedi: {ex.Message}", "Tamam");
				Debug.WriteLine($"Dosya kaydetme hatası: {ex.Message}");
			}
			
			// Dosyayı paylaş
			var shareQuestion = await DisplayAlert("Paylaşım", "Excel raporunu paylaşmak ister misiniz?", "Evet", "Hayır");
			if (shareQuestion)
			{
				await Share.RequestAsync(new ShareFileRequest
				{
					Title = "Anket Raporu",
					File = new ShareFile(tempFilePath)
				});
			}
			
			Debug.WriteLine($"Excel raporu oluşturuldu: {tempFilePath}");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Excel raporu oluşturulurken hata: {ex.Message}");
			await DisplayAlert("Hata", "Excel raporu oluşturulurken bir hata oluştu: " + ex.Message, "Tamam");
		}
		finally
		{
			// Yükleme göstergesini gizle
			await ShowLoadingIndicator(false);
		}
	}
	
	private void CreateExcelFile(ReportResults results, DateTime startDate, DateTime endDate, string filePath)
	{
		using (var workbook = new XLWorkbook())
		{
			// Çalışma sayfası oluştur
			var worksheet = workbook.AddWorksheet("Anket Raporu");
			
			// Başlık oluştur
			worksheet.Cell("A1").Value = "ÖZFİLİZ YAZILIM EXCEL ANKET RAPORU";
			worksheet.Range("A1:E1").Merge();
			worksheet.Range("A1:E1").Style.Font.Bold = true;
			worksheet.Range("A1:E1").Style.Font.FontSize = 14;
			worksheet.Range("A1:E1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			
			// Tarih aralığı bilgisi
			worksheet.Cell("A2").Value = $"BAŞLANGIÇ TARİH ARALIĞI: {startDate:dd.MM.yyyy} BAŞLANGIÇ SAATİ: {startDate:HH:mm:ss}";
			worksheet.Range("A2:E2").Merge();
			worksheet.Range("A2:E2").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			
			worksheet.Cell("A3").Value = $"BİTİŞ TARİH ARALIĞI: {endDate:dd.MM.yyyy} BİTİŞ SAATİ: {endDate:HH:mm:ss}";
			worksheet.Range("A3:E3").Merge();
			worksheet.Range("A3:E3").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
			
			// Özet Rapor başlığı
			worksheet.Cell("A5").Value = "ÖZET RAPOR:";
			worksheet.Range("A5:E5").Merge();
			worksheet.Range("A5:E5").Style.Font.Bold = true;
			worksheet.Range("A5:E5").Style.Font.FontSize = 12;
			
			// Toplam oy sayıları ve yüzdelikleri hesapla
			int totalVotes = results.MutluCount + results.NotrCount + results.UzgunCount;
			double mutluYuzde = 0;
			double notrYuzde = 0;
			double uzgunYuzde = 0;
			
			if (totalVotes > 0)
			{
				mutluYuzde = (double)results.MutluCount / totalVotes * 100;
				notrYuzde = (double)results.NotrCount / totalVotes * 100;
				uzgunYuzde = (double)results.UzgunCount / totalVotes * 100;
			}
			
			// Özet bilgiler
			worksheet.Cell("A6").Value = $"MUTLU - {results.MutluCount} - %{mutluYuzde:F2}";
			worksheet.Cell("A7").Value = $"KARARSIZ - {results.NotrCount} - %{notrYuzde:F2}";
			worksheet.Cell("A8").Value = $"MUTSUZ - {results.UzgunCount} - %{uzgunYuzde:F2}";
			
			// Detay Rapor başlığı
			worksheet.Cell("A10").Value = "DETAY RAPOR:";
			worksheet.Range("A10:E10").Merge();
			worksheet.Range("A10:E10").Style.Font.Bold = true;
			worksheet.Range("A10:E10").Style.Font.FontSize = 12;
			
			// Detay tablo başlıkları
			worksheet.Cell("A11").Value = "TARİH";
			worksheet.Cell("B11").Value = "OY TÜRÜ";
			
			worksheet.Range("A11:B11").Style.Font.Bold = true;
			worksheet.Range("A11:B11").Style.Fill.BackgroundColor = XLColor.LightGray;
			worksheet.Range("A11:B11").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
			
			// Kayıtları ekle - öncelikle tüm kayıtları bir listeye topla ve sırala
			var allRecords = new List<(DateTime date, string oyTuru)>();
			
			// SQLite kayıtlarını ekle
			foreach (var record in results.SqliteRecords)
			{
				string oyTuru = record.AnketID switch
				{
					1001 => "MUTLU",
					1002 => "KARARSIZ",
					1003 => "MUTSUZ",
					_ => "BİLİNMİYOR"
				};
				
				allRecords.Add((record.Date, oyTuru));
			}
			
			// Firebase kayıtlarını ekle (SQLite'da olmayanları)
			foreach (var record in results.FirebaseRecords)
			{
				// Tarih formatlama için parse et
				if (DateTime.TryParse(record.CreateDate, out DateTime recordDate))
				{
					// Cihaz ID ve tarih kombinasyonu ile SQLite'da var mı kontrol et
					bool isDuplicate = results.SqliteRecords.Any(r => 
						r.DeviceID == record.DeviceID && 
						Math.Abs((r.Date - recordDate).TotalSeconds) < 5); // 5 saniyelik tolerans
					
					// Eğer bu kayıt SQLite'da yoksa ekle
					if (!isDuplicate)
					{
						string oyTuru = record.MemnunInd switch
						{
							"0" => "MUTLU",
							"1" => "KARARSIZ",
							"2" => "MUTSUZ",
							_ => "BİLİNMİYOR"
						};
						
						allRecords.Add((recordDate, oyTuru));
					}
				}
			}
			
			// Tarih bazında sırala (en yeni en üstte)
			allRecords = allRecords.OrderByDescending(r => r.date).ToList();
			
			// Kayıtları Excel'e ekle
			for (int i = 0; i < allRecords.Count; i++)
			{
				var record = allRecords[i];
				int row = i + 12; // 12. satırdan başla
				
				worksheet.Cell(row, 1).Value = record.date.ToString("dd.MM.yyyy HH:mm:ss");
				worksheet.Cell(row, 2).Value = record.oyTuru;
				
				// Hücre sınırları
				worksheet.Range(row, 1, row, 2).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				
				// Renklendirme
				if (record.oyTuru == "MUTLU")
				{
					worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightGreen;
				}
				else if (record.oyTuru == "KARARSIZ")
				{
					worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightYellow;
				}
				else if (record.oyTuru == "MUTSUZ")
				{
					worksheet.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.LightPink;
				}
			}
			
			// Sütun genişliklerini otomatik ayarla
			worksheet.Columns().AdjustToContents();
			
			// Excel dosyasını kaydet
			workbook.SaveAs(filePath);
		}
	}
}