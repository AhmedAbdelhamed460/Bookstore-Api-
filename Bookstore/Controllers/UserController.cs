﻿using Bookstore.Models;
using Bookstore.DOT;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MimeKit;
using MimeKit.Text;
using MailKit.Net.Smtp;
using MailKit.Security;
using Bookstore.Services;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json.Linq;
using System.Web;

namespace Bookstore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IEmailSender _emailSender;

        public UserController(UserManager<AppUser> userManager, IEmailSender emailSender)
        {
            this.userManager = userManager;
            this._emailSender = emailSender;
        }

        [HttpGet]
        public async Task<ActionResult> getAllUsers()
        {
            List<AppUser> users = await userManager.Users.ToListAsync();
            if (users.Count != 0) return Ok(users);
            else return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult> Registration(RegistrationDTO registrationDTO)
        {
            if (ModelState.IsValid)
            {
                AppUser user = new()
                {
                    firstName = registrationDTO.FirstName,
                    LastName = registrationDTO.LastName,
                    UserName = registrationDTO.UserName,
                    Email = registrationDTO.Email,
                    PhoneNumber = registrationDTO.PhoneNumber,
                    Address = registrationDTO.Address             
                };
                IdentityResult addUserResult = await userManager.CreateAsync(user, registrationDTO.Password);
                IdentityResult addRoleToUserResult = await userManager.AddToRoleAsync(user, "Customer");
                registrationDTO.Id = user.Id;
                if (addUserResult.Succeeded && addRoleToUserResult.Succeeded) 
                {
                    return Ok(registrationDTO);
                } 
                else return BadRequest();
            }
            return BadRequest();
        }  

        [HttpPost("/api/userlogin")]
        public async Task<ActionResult> Login(LoginDTO loginDTO)
        {
                AppUser? user = await userManager.FindByEmailAsync(loginDTO.Email);
                if (user != null)
                {
                    bool valid = await userManager.CheckPasswordAsync(user, loginDTO.Password);
                    if (valid)
                    {
                        //Generate Token
                        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my_secret_key_123456"));
                        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                        var claims = new List<Claim>();
                        claims.Add(new Claim(ClaimTypes.Name, user.UserName));
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
                        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));


                        //Get Role
                        var userRoles = await userManager.GetRolesAsync(user);
                        foreach(var userRole in userRoles)
                        {
                         claims.Add(new Claim(ClaimTypes.Role, userRole));
                        }


                        var myToken = new JwtSecurityToken(
                        expires: DateTime.Now.AddMinutes(120),
                        signingCredentials: credentials,
                        claims: claims   );

                        return Ok(new
                        {
                            token = new JwtSecurityTokenHandler().WriteToken(myToken),
                            expiration= myToken.ValidTo
                            //roles = userRoles
                        });


                    }
                    else return Unauthorized();
                }
                return Unauthorized();

        }

        [HttpPost("/api/userToRole")]
        public async Task<ActionResult> addRoleToUser(RoleToUserDTO roleToUserDTO)
        {
            if (ModelState.IsValid)
            {
                foreach (var userId in roleToUserDTO.UserIds)
                {
                    AppUser user = await userManager.FindByIdAsync(userId);
                    IdentityResult result = await userManager.AddToRolesAsync(user , roleToUserDTO.RoleNames);
                    if (result.Succeeded == false)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        return BadRequest();
                    }                                     
                }
                return Ok(roleToUserDTO);
            }
            return BadRequest();
        }

        [HttpPost("/api/forgotPassword/{email}")]
        public async Task<ActionResult> forgotPassword(string email)

        {
            if (ModelState.IsValid)
            {
                AppUser? user = await userManager.FindByEmailAsync(email);
                if (user != null)
                {
                   
                 var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    //token = HttpUtility.UrlEncode(token);
                    var link = Url.Action(nameof(ResetPassword), "User", new { token, email = user.Email }, Request.Scheme);

                    EmailDto request = new EmailDto()
                    {
                        To = email,
                        Subject = "Reset Password",
                        Body = EmailBody.EmailStringBody(email, token)
                    };  
                 _emailSender.SendEmail(request);
                 return Ok("Email Sent");

                }
                return BadRequest("user not found");
            }
            return BadRequest("model state");

        }

        [HttpPost("/api/resetPassword")]
        public async Task<ActionResult> ResetPassword(ResetPasswordDTO resetPasswordDTO)
        {
            if (ModelState.IsValid)
            {
                AppUser? user = await userManager.FindByEmailAsync(resetPasswordDTO.Email);
                if (user != null)
                {
                    //resetPasswordDTO.EmailToken = HttpUtility.UrlDecode(resetPasswordDTO.EmailToken);
                    IdentityResult resetPassResult = await userManager.ResetPasswordAsync(user, resetPasswordDTO.EmailToken, resetPasswordDTO.NewPassword);

                    if (resetPassResult.Succeeded == false)
                    {
                        foreach (var error in resetPassResult.Errors)
                        {
                            ModelState.TryAddModelError(error.Code, error.Description);
                        }
                        return BadRequest("identity result");
                    }
                    return Ok();
                }
                return BadRequest("user not found");
            }
            return BadRequest("model state");

        }

    }

}

