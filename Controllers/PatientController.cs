using MenengiomaBackend.Data;
using MenengiomaBackend.Models;
using MenengiomaBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MenengiomaBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PatientController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatientController(AppDbContext context)
        {
            _context = context;
        }

        // 1. Yeni Hasta Ekleme (Flutter'dan gelen veriyi kaydeder)
        [HttpPost]
        public async Task<IActionResult> AddPatient(PatientCreateDto request)
        {
            // Bu TC Kimlik Numarası ile daha önce kayıt yapılmış mı kontrol et
            if (await _context.Patients.AnyAsync(p => p.TCIdentityNo == request.TCIdentityNo))
            {
                return BadRequest("Bu TC Kimlik Numarası ile kayıtlı bir hasta zaten var.");
            }

            // DTO'dan gelen verileri gerçek Patient modeline dönüştür
            var newPatient = new Patient
            {
                TCIdentityNo = request.TCIdentityNo,
                FirstName = request.FirstName,
                LastName = request.LastName,
                // PostgreSQL zaman krizini yaşamamak için tarihi yine UTC'ye çeviriyoruz:
                BirthDate = request.BirthDate.ToUniversalTime(), 
                Gender = request.Gender
            };

            // Veritabanına ekle
            _context.Patients.Add(newPatient);
            await _context.SaveChangesAsync();

            return Ok($"Hasta başarıyla eklendi! Hasta ID: {newPatient.PatientID}");
        }

        // 2. Tüm Hastaları Listeleme (Flutter'da liste ekranında gösterir)
        [HttpGet]
        public async Task<IActionResult> GetAllPatients()
        {
            var patients = await _context.Patients.ToListAsync();
            return Ok(patients);
        }
    }
}