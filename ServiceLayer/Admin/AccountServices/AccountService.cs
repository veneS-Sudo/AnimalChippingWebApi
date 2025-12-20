using AnimalChippingWebApi.Core.Models;
using AnimalChippingWebApi.DataBaseLayer.Context;
using DatabaseLayer.QueryObjects;
using DatabaseLayer.Validators;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Admin.Dto;

namespace ServiceLayer.Admin.AccountServices;

public class AccountService : IAccountService
{
   private readonly AppDbContext _context;
   private readonly IGenericValidator<Account> _accountValidator;

   public AccountService(
      AppDbContext context,
      IGenericValidator<Account> accountValidator)
   {
      _context = context;
      _accountValidator = accountValidator;
   }

   public Account GetAccountById(int accountId)
   {
      CheckIfAccountExists(accountId);
      
      var account = _context.Accounts
         .AsNoTracking()
         .Single(a => a.AccountId == accountId);
      return account;
   }

   public IQueryable<Account> GetAccounts(AccountSortFilterPageOptions filterOptions)
   {
      var query = _context.Accounts
         .AsNoTracking()
         .FilterAccountsBy(filterOptions.filterValues)
         .OrderBy(a => a.AccountId);
      
      filterOptions.SetupRest(query);
      
      return query.Page(filterOptions.PageNum-1, filterOptions.PageSize);
   }

   public bool UpdateAccountById(int accountId, UpdateAccountDto dto)
   {
      CheckIfAccountExists(accountId);
      if (string.IsNullOrWhiteSpace(dto.FirstName)
          || string.IsNullOrWhiteSpace(dto.LastName)
          || string.IsNullOrWhiteSpace(dto.Email)
          || string.IsNullOrWhiteSpace(dto.Password))
         throw new Exception("Invalid data");
      
      var account = _context.Accounts
         .AsNoTracking()
         .Single(a => a.AccountId == accountId);
      if (_context.Accounts.Any(a => a.Email == dto.Email))
         throw new Exception($"Email {dto.Email} already exists");
      
      account.FirstName = dto.FirstName;
      account.LastName = dto.LastName;
      account.Email = dto.Email;
      account.Password = dto.Password;
      
      var result = _context.SaveChanges();
      return result > 0;
   }

   public bool DeleteAccountById(int accountId)
   {
      CheckIfAccountExists(accountId);
      var account = _context.Accounts
         .Include(a => a.ChippedAnimals)
         .Single(a => a.AccountId == accountId);
      if (account.ChippedAnimals.Any())
         throw new Exception($"Account {accountId} has chipped animals");
      
      _context.Accounts.Remove(account);
      var result = _context.SaveChanges();
      return result > 0;
   }

   private void CheckIfAccountExists(int accountId)
   {
      _accountValidator.CheckWithThrowExceptions(new Account() {AccountId = accountId},
         $"Account with id {accountId} not found.");
   }
}