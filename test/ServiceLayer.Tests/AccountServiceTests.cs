using System.Collections;
using DatabaseLayer.Context;
using DatabaseLayer.Models;
using DatabaseLayer.Validators;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using ServiceLayer.Admin.AccountServices;
using ServiceLayer.Admin.Dto;
using ServiceLayer.Registration.Exceptions;

namespace ServiceLayer.Tests;

public class AccountServiceTests
{
    private DbContextOptions<AppDbContext> Open()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        return options;
    }

    [Theory]
    [InlineData(5, 5,  10)]
    [InlineData(2, 2, 10)]
    [InlineData(9, 9,  10)]
    [InlineData(15, 10, 10)]
    [InlineData(30, 25, 25)]
    [InlineData(5, 5, 1)]
    public void GetAccounts_ShouldReturnExpectedCountAccounts(int count, int result, int size)
    {

        var options = Open();
        IEnumerable<Account> arrange;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            arrange = Enumerable.Range(1, count)
                .Select(i => new Account() 
                    { AccountId = i, FirstName = $"FirstName{i}", LastName = $"LastName{i}", Email = $"Email{i}",
                        Password = string.Concat(Enumerable.Repeat(i.ToString(), i))});
            context.Accounts.AddRange(arrange);
            context.SaveChanges();
        }

        using (var context = new AppDbContext(options))
        {
            var accountValidatorMock = new Mock<IGenericValidator<Account>>();
            var service = new AccountService(context, accountValidatorMock.Object);
            var searchOptions = new AccountSortFilterPageOptions() {PageNum = 1, PageSize = size};
            var accounts = service.GetAccounts(searchOptions);
            Assert.Equal(result, accounts.Count());
            //Assert.Equal(arrange, accounts, new AccountEqualityComparer());
        }
    }

    [Theory]
    [ClassData(typeof(AccountInvalidData))]
    public void UpdateAccountById_ShouldThrowInvalidOperationException(UpdateAccountDto dto)
    {
        var options = Open();
        int accountId;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account { Email = "e", FirstName = "f", LastName = "l", Password = "p" };
            context.Accounts.Add(account);
            context.SaveChanges();
            accountId = account.AccountId;
        }

        using (var context = new AppDbContext(options))
        {
            var accountValidatorMock = new Mock<IGenericValidator<Account>>();
            var service = new AccountService(context, accountValidatorMock.Object);
            Assert.Throws<InvalidOperationException>(() => service.UpdateAccountById(accountId, dto));
        }
    }

    [Fact]
    public void UpdateAccountById_ShouldThrowUniqueValueException()
    {
        var options = Open();
        int accountId;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account { Email = "test", FirstName = "test", LastName = "test", Password = "test" };
            context.Accounts.AddRange(account,
                new Account { Email = "email", FirstName = "first", LastName = "last", Password = "password" });
            context.SaveChanges();
            accountId = account.AccountId;
        }

        using (var context = new AppDbContext(options))
        {
            var accountValidatorMock = new Mock<IGenericValidator<Account>>();
            var dto = new UpdateAccountDto() { Email = "email", FirstName = "test", LastName = "test", Password = "password" };
            var service = new AccountService(context, accountValidatorMock.Object);
            Assert.Throws<UniqueValueException>(() => service.UpdateAccountById(accountId, dto));
        }
    }

    [Fact]
    public void DeleteAccountById_ShouldThrowInvalidOperationException()
    {
        
        var options = Open();
        int accountId;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var animalType = new AnimalType { Type = "nail"};
            var point = new LocationPoint();
            context.Locations.Add(point);
            context.AnimalTypes.Add(animalType);
            var animal = new Animal()
                { AnimalTypes = new List<AnimalType>() { animalType }, AccountId = 1, ChippingLocationPoint = point };
            context.Animals.Add(animal);
            var account = new Account
            {
                AccountId = 1, Email = "test", FirstName = "test", LastName = "test", Password = "test",
                ChippedAnimals = new List<Animal>() { animal }
            };
            context.Accounts.Add(account);
            context.SaveChanges();
            accountId = account.AccountId;
        }

        using (var context = new AppDbContext(options))
        {
            var accountValidatorMock = new Mock<IGenericValidator<Account>>();
            var service = new AccountService(context, accountValidatorMock.Object);
            Assert.Throws<InvalidOperationException>(() => service.DeleteAccountById(accountId));
        }
    }

    [Fact]
    public void DeleteAccountById_ShouldDeleteAccount()
    {
        var options = Open();
        int accountId;
        using (var context = new AppDbContext(options))
        {
            context.Database.EnsureCreated();
            var account = new Account
            {
                AccountId = 1, Email = "test", FirstName = "test", LastName = "test", Password = "test",
            };
            context.Accounts.Add(account);
            context.SaveChanges();
            accountId = account.AccountId;
        }

        using (var context = new AppDbContext(options))
        {
            var accountValidatorMock = new Mock<IGenericValidator<Account>>();
            var service = new AccountService(context, accountValidatorMock.Object);
            var result = service.DeleteAccountById(accountId);
            Assert.True(result);
            Assert.Null(context.Accounts.FirstOrDefault(x => x.AccountId == accountId));
            Assert.Null(service.GetAccountById(accountId));
        }
    }

    private class AccountInvalidData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[]
                { new UpdateAccountDto() { Email = "email", Password = "password", LastName = "lastname" } };
            yield return new object[]
                { new UpdateAccountDto() { Email = "email", Password = "password", FirstName = "firstname"} };
            yield return new object[]
                { new UpdateAccountDto() { Email = "email", FirstName = "firstname", LastName = "lastname" } };
            yield return new object[]
                { new UpdateAccountDto() { Password = "password", FirstName = "firstname", LastName = "lastname" } };
            yield return new object[]
                { new UpdateAccountDto() };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    internal class AccountEqualityComparer : IEqualityComparer<Account>
    {
        public bool Equals(Account? x, Account? y)
        {
            if (x is null || y is null)
                return false;
            return x.AccountId == y.AccountId;
        }

        public int GetHashCode(Account obj)
        {
            var hashCode = obj.AccountId.GetHashCode();
            return hashCode;
        }
    }
}