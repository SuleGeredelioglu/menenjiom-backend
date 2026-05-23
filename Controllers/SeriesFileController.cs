using MenengiomaBackend.Data;
using MenengiomaBackend.Models;
using MenengiomaBackend.DTOs;
using MenengiomaBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace MenengiomaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SeriesFileController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly AiIntegrationService _aiService;

        public SeriesFileController(AppDbContext context, AiIntegrationService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        // 1. Yeni Dosya/Sonuç Ekleme (Manuel)
        [HttpPost]
        public async Task<IActionResult> AddSeriesFile(SeriesFileCreateDto request)
        {
            var studyExists = await _context.Studies.AnyAsync(s => s.StudyID == request.StudyID);
            if (!studyExists)
            {
                return NotFound(new { message = "Hata: Belirtilen ID'ye sahip bir MR çekimi bulunamadı!" });
            }

            var newSeriesFile = new SeriesFile
            {
                StudyID = request.StudyID,
                AiReportContent = request.AiReportContent,
                FilePath_Original = request.FilePath_Original,
                FilePath_Mask = request.FilePath_Mask,
                TumorVolume = request.TumorVolume,
                IsProcessed = request.IsProcessed
            };

            _context.SeriesFiles.Add(newSeriesFile);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                seriesID = newSeriesFile.SeriesID,
                message = "Dosya kaydı başarıyla eklendi!"
            });
        }

        // 2. Belirli Bir MR Çekimine Ait Dosyaları Getirme
        [HttpGet("study/{studyId}")]
        public async Task<IActionResult> GetFilesByStudy(int studyId)
        {
            var files = await _context.SeriesFiles
                .Where(f => f.StudyID == studyId)
                .ToListAsync();

            if (!files.Any())
            {
                return NotFound(new { message = "Bu MR çekimine ait herhangi bir dosya kaydı bulunamadı." });
            }

            return Ok(files);
        }

        // -------------------------------------------------------------------
        // 3. YAPAY ZEKA ENTEGRASYONU (ÜSTÜNE YAZMA SORUNU ÇÖZÜLDÜ)
        // -------------------------------------------------------------------
        [HttpPost("{studyId}/analyze")]
        public async Task<IActionResult> AnalyzeAndSaveAiReport(int studyId, IFormFile mriZipFile)
        {
            if (mriZipFile == null || mriZipFile.Length == 0)
                return BadRequest(new { message = "Lütfen analiz için geçerli bir DICOM ZIP dosyası yükleyin." });

            // ÇOK ÖNEMLİ: Artık eski dosyayı aramıyoruz, YENİ bir satır (kayıt) oluşturuyoruz!
            // Flutter'dan gelen ID'yi artık "StudyID" (Çekim Grubu ID'si) olarak kullanıyoruz.
            var newSeriesFile = new SeriesFile
            {
                StudyID = studyId,
                FilePath_Original = mriZipFile.FileName,
                IsProcessed = false // İşlem yeni başladı
            };

            // Önce yeni satırı veritabanına ekle ki otomatik bir SeriesID (Örn: 2, 3, 4) alsın
            _context.SeriesFiles.Add(newSeriesFile);
            await _context.SaveChangesAsync();

            try
            {
                // Python AI servisine gönder
                var aiResult = await _aiService.AnalyzeMriAsync(mriZipFile);

                // AI analizi bittikten sonra yeni oluşturduğumuz satırı sonuçlarla güncelliyoruz
                if (aiResult?.Volumes_cm3 != null)
                {
                    newSeriesFile.TumorVolume = (float)aiResult.Volumes_cm3.Total_wt;
                    newSeriesFile.FilePath_Mask = aiResult.Mask_file_path;
                    newSeriesFile.IsProcessed = true; // İşlem bitti

                    newSeriesFile.AiReportContent = $"Otomatik Analiz Sonucu: Nekrotik Çekirdek {aiResult.Volumes_cm3.Ncr} cm³, " +
                                                 $"Ödem {aiResult.Volumes_cm3.Ed} cm³, Aktif Tümör {aiResult.Volumes_cm3.Et} cm³.";

                    _context.SeriesFiles.Update(newSeriesFile);
                    await _context.SaveChangesAsync();
                }

                // Flutter'a yanıt dön
                return Ok(new
                {
                    status = "success",
                    message = "Yapay zeka analizi başarıyla tamamlandı.",
                    series_id = newSeriesFile.SeriesID, // Yeni oluşturulan ID'yi de dönüyoruz
                    data = new
                    {
                        volumes_cm3 = new
                        {
                            ncr = aiResult?.Volumes_cm3?.Ncr ?? 0,
                            ed = aiResult?.Volumes_cm3?.Ed ?? 0,
                            et = aiResult?.Volumes_cm3?.Et ?? 0,
                            total_wt = aiResult?.Volumes_cm3?.Total_wt ?? 0
                        },
                        mask_file_path = aiResult?.Mask_file_path
                    }
                });
            }
            catch (Exception ex)
            {
                // Eğer yapay zeka hata verirse, oluşturduğumuz satıra hata durumunu yazabiliriz
                newSeriesFile.AiReportContent = $"Analiz Hatası: {ex.Message}";
                _context.SeriesFiles.Update(newSeriesFile);
                await _context.SaveChangesAsync();

                return StatusCode(500, new { message = $"AI Sunucusu ile iletişim hatası: {ex.Message}" });
            }
        }
    }
}