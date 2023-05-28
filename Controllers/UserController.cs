//using Aton.Models;
//using Microsoft.AspNetCore.Mvc;

//namespace Aton.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class UsersController : ControllerBase
//    {
//        private readonly IUserRepository _userRepository;

//        public UsersController(IUserRepository userRepository)
//        {
//            _userRepository = userRepository;
//        }

//        // CRUD методы

//        [HttpPost]
//        public IActionResult Create(string login, string password, string name, int gender, DateTime? birthday, bool admin)
//        {
//            // Проверка параметров логина и пароля выполняющего запрос
//            // ...

//            // Создание нового пользователя
//            User user = new User
//            {
//                Login = login,
//                Password = password,
//                Name = name,
//                Gender = gender,
//                Birthday = birthday,
//                Admin = admin
//            };

//            _userRepository.Create(user);

//            return Ok();
//        }

//        [HttpPut("{id}")]
//        public IActionResult Update(Guid id, string login, string password, string name, int gender, DateTime? birthday)
//        {
//            // Проверка параметров логина и пароля выполняющего запрос
//            // ...

//            User user = _userRepository.GetById(id);
//            if (user == null)
//                return NotFound();

//            // Изменение имени, пола или даты рождения пользователя
//            user.Name = name;
//            user.Gender = gender;
//            user.Birthday = birthday;

//            _userRepository.Update(user);

//            return Ok();
//        }

//        // Реализация остальных методов контроллера

//        // ...
//    }

//}
