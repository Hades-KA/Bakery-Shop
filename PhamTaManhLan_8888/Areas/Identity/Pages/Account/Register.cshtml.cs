// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using PhamTaManhLan_8888.Areas.Admin.Models;
using PhamTaManhLan_8888.Models;

namespace PhamTaManhLan_8888.Areas.Identity.Pages.Account
{
	public class RegisterModel : PageModel
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly RoleManager<IdentityRole> _roleManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUserStore<ApplicationUser> _userStore;
		private readonly IUserEmailStore<ApplicationUser> _emailStore;
		private readonly ILogger<RegisterModel> _logger;
		private readonly IEmailSender _emailSender;

		public RegisterModel(
			UserManager<ApplicationUser> userManager,
			RoleManager<IdentityRole> roleManager,
			IUserStore<ApplicationUser> userStore,
			SignInManager<ApplicationUser> signInManager,
			ILogger<RegisterModel> logger,
			IEmailSender emailSender)
		{
			_roleManager = roleManager;
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
			_emailSender = emailSender;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public string ReturnUrl { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public class InputModel
		{
			[Required]
			public string FullName { get; set; }

			[Required]
			[EmailAddress]
			[Display(Name = "Email")]
			public string Email { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự và tối đa {1} ký tự.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "Mật khẩu")]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Xác nhận mật khẩu")]
			[Compare("Password", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
			public string ConfirmPassword { get; set; }

			[Required]
			[Phone]
			[Display(Name = "Số điện thoại")]
			public string PhoneNumber { get; set; }

			public string? Role { get; set; }
			[ValidateNever]
			public IEnumerable<SelectListItem> RoleList { get; set; }
		}

		public async Task OnGetAsync(string returnUrl = null)
		{
			ReturnUrl = returnUrl;
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (!_roleManager.RoleExistsAsync(SD.Role_Customer).GetAwaiter().GetResult())
			{
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_Customer));
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_Employee));
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));
				await _roleManager.CreateAsync(new IdentityRole(SD.Role_Company));
			}

			Input = new InputModel
			{
				RoleList = _roleManager.Roles.Select(x => new SelectListItem
				{
					Text = x.Name,
					Value = x.Name
				}).ToList()
			};
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (ModelState.IsValid)
			{
				var user = CreateUser();
				user.FullName = Input.FullName;
				user.PhoneNumber = Input.PhoneNumber;

				await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
				await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

				var result = await _userManager.CreateAsync(user, Input.Password);

				if (result.Succeeded)
				{
					_logger.LogInformation("Tài khoản mới đã được tạo thành công.");

					if (!string.IsNullOrEmpty(Input.Role) && await _roleManager.RoleExistsAsync(Input.Role))
					{
						await _userManager.AddToRoleAsync(user, Input.Role);
					}
					else
					{
						await _userManager.AddToRoleAsync(user, "Customer");
					}

					await _signInManager.SignInAsync(user, isPersistent: false);
					return LocalRedirect(returnUrl);
				}

				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			return Page();
		}

		private ApplicationUser CreateUser()
		{
			return Activator.CreateInstance<ApplicationUser>();
		}

		private IUserEmailStore<ApplicationUser> GetEmailStore()
		{
			if (!_userManager.SupportsUserEmail)
			{
				throw new NotSupportedException("Hệ thống yêu cầu kho lưu trữ người dùng hỗ trợ email.");
			}
			return (IUserEmailStore<ApplicationUser>)_userStore;
		}
	}
}
