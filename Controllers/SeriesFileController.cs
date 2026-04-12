using MenengiomaBackend.Data;
using MenengiomaBackend.Models;
using MenengiomaBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenengiomaBackend.Controllers
{
    // API rotasını belirler (api/SeriesFile)
    [Route("api/[controller]")]
    [ApiController]
    public class SeriesFileController : ControllerBase
    {
        private readonly AppDbContext _context;

        // Veritabanı bağlamını (context) constructor üzerinden enjekte eder
        public SeriesFileController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Yeni Dosya/Sonuç Ekleme
        // Flutter'dan gelen analiz sonuçlarını ve raporu veritabanına kaydeder
        [HttpPost]
        public async Task<IActionResult> AddSeriesFile(SeriesFileCreateDto request)
        {
            // Veri bütünlüğü için veritabanında belirtilen MR çekiminin (Study) varlığını kontrol eder
            var studyExists = await _context.Studies.AnyAsync(s => s.StudyID == request.StudyID);
            if (!studyExists)
            {
                // Hata mesajını da JSON formatında dönmek, modern API'ler için daha sağlıklıdır.
                return NotFound(new { message = "Hata: Belirtilen ID'ye sahip bir MR çekimi bulunamadı!" });
            }

            // DTO'dan gelen verileri gerçek veritabanı modeline (Entity) eşler
            var newSeriesFile = new SeriesFile
            {
                StudyID = request.StudyID,
                AiReportContent = request.AiReportContent,
                FilePath_Original = request.FilePath_Original,
                FilePath_Mask = request.FilePath_Mask,
                TumorVolume = request.TumorVolume,
                IsProcessed = request.IsProcessed
            };

            // Yeni kaydı tabloya ekler ve değişiklikleri diske yazar
            _context.SeriesFiles.Add(newSeriesFile);
            await _context.SaveChangesAsync();

            // KODUN GÜNCELLENEN TEK KISMI:
            // İşlem başarılıysa yeni oluşturulan kaydın ID'sini JSON formatında döndürür.
            // Bu sayede Flutter tarafındaki jsonDecode(response.body) fonksiyonu sorunsuz çalışır.
            return Ok(new
            {
                seriesID = newSeriesFile.SeriesID,
                message = "Dosya kaydı başarıyla eklendi!"
            });
        }

        // 2. Belirli Bir MR Çekimine Ait Dosyaları/Sonuçları Getirme
        // "Geçmiş Raporlar" ekranı için ilgili tetkike ait tüm kayıtları listeler
        [HttpGet("study/{studyId}")]
        public async Task<IActionResult> GetFilesByStudy(int studyId)
        {
            // StudyID eşleşmesine göre ilgili tüm raporları sorgular
            var files = await _context.SeriesFiles
                .Where(f => f.StudyID == studyId)
                .ToListAsync();

            // Eğer listede hiç kayıt yoksa kullanıcıya bilgi mesajı döner
            if (!files.Any())
            {
                return NotFound(new { message = "Bu MR çekimine ait herhangi bir dosya kaydı bulunamadı." });
            }

            // Kayıtlar bulunduysa JSON formatında listeyi döndürür
            return Ok(files);
        }
    }
}