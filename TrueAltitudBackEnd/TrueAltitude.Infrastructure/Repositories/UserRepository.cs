using Microsoft.EntityFrameworkCore;
using TrueAltitude.Domain.Entities;
using TrueAltitude.Infrastructure.Interfaces;
using TrueAltitude.Persistence.Data;

namespace TrueAltitude.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly TrueAltitudeDbContext _context;

    public UserRepository(TrueAltitudeDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User> CreateAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
