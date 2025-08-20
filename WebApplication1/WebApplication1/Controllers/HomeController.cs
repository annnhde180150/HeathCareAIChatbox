using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using BusinessObjects.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HeathcareSystemContext _context;

        public HomeController(ILogger<HomeController> logger, HeathcareSystemContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.Password);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    return RedirectToAction("Chat", "Home");
                }
                ModelState.AddModelError(string.Empty, "Wrong email or passw.");
            }
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetChatSessions()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var chatSessions = await _context.ChatSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();

            return Ok(chatSessions);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetChatMessages(int sessionId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.UserId == userId);

            if (chatSession == null)
            {
                return NotFound("Chat session not found or not authorized.");
            }

            var messages = await _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();

            return Ok(messages);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateChatSession()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var newSession = new ChatSession
            {
                UserId = userId,
                StartedAt = DateTime.UtcNow,
                EndedAt = null
            };

            _context.ChatSessions.Add(newSession);
            await _context.SaveChangesAsync();

            return Ok(newSession);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessageRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized();
            }

            var chatSession = await _context.ChatSessions
                .FirstOrDefaultAsync(s => s.SessionId == request.SessionId && s.UserId == userId);

            if (chatSession == null)
            {
                return NotFound("Chat session not found or not authorized.");
            }

            var newMessage = new ChatMessage
            {
                SessionId = request.SessionId,
                Sender = request.Sender,
                MessageText = request.MessageText,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatMessages.Add(newMessage);
            await _context.SaveChangesAsync();

            return Ok(newMessage);
        }

        [Authorize]
        public IActionResult Chat()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public class ChatMessageRequest
        {
            public int SessionId { get; set; }
            public string Sender { get; set; } = null!;
            public string MessageText { get; set; } = null!;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> GetAIResponse([FromBody] AIRequest request)
        {
            // Here you would integrate with your actual AI service (e.g., call a Python script, an external API, etc.)
            // For now, let's return a simple placeholder response.
            var aiResponseText = $"AI received: '{request.Message}' and will respond soon.";
            return Ok(new AIResponse { ResponseText = aiResponseText });
        }

        public class AIRequest
        {
            public string Message { get; set; } = null!;
        }

        public class AIResponse
        {
            public string ResponseText { get; set; } = null!;
        }
    }
}
