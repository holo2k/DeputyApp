﻿using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations;

public class PostService : IPostService
{
    private readonly IUnitOfWork _uow;

    public PostService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Post?> GetByIdAsync(Guid id)
    {
        return await _uow.Posts.GetByIdAsync(id, p => EF.Property<object>(p, "Attachments"));
    }


    public async Task<IEnumerable<Post>> GetPublishedAsync(int skip = 0, int take = 20)
    {
        return await _uow.Posts.ListAsync(p => p.PublishedAt != null, skip, take);
    }


    public async Task<Post> CreateAsync(Post post)
    {
        post.Id = Guid.NewGuid();
        post.CreatedAt = DateTimeOffset.UtcNow;
        post.PublishedAt = null;
        await _uow.Posts.AddAsync(post);
        await _uow.SaveChangesAsync();
        return post;
    }


    public async Task PublishAsync(Guid postId)
    {
        var p = await _uow.Posts.GetByIdAsync(postId);
        if (p == null) throw new KeyNotFoundException("Post not found");
        p.PublishedAt = DateTimeOffset.UtcNow;
        _uow.Posts.Update(p);
        await _uow.SaveChangesAsync();
    }


    public async Task DeleteAsync(Guid postId)
    {
        var p = await _uow.Posts.GetByIdAsync(postId);
        if (p == null) return;
        _uow.Posts.Delete(p);
        await _uow.SaveChangesAsync();
    }
}