using Application.Dtos;
using Application.Services.Abstractions;
using DeputyApp.DAL.UnitOfWork;
using Domain.Entities;
using Infrastructure.DAL.Repository.Abstractions;

namespace Application.Services.Implementations;

public class TaskService(HttpClient httpClient, IAuthService auth, IUnitOfWork uow) : ITaskService
{
    private readonly ITaskRepository taskRepository = uow.Tasks;
    public async Task<Guid> CreateAsync(CreateTaskRequest request)
    {
        var currentUserId = auth.GetCurrentUserId();
        var entity = new TaskEntity
        {
            AuthorId = currentUserId,
            Description = request.Description,
            ExpectedEndDate = request.ExpectedEndDate,
            Priority = request.Priority,
            StatusId = request.StatusId,
            Title = request.Title,
        };
        entity.AuthorId = currentUserId;
        await taskRepository.AddAsync(entity);
        
        return entity.Id;
    }

    public async Task Delete(Guid id)
    {
        var entity = await taskRepository.GetByIdAsync(id);
        if (entity is null) throw new Exception($"Task with id {id} was not found");
        uow.Tasks.Delete(entity);
    }

    public async Task<IEnumerable<TaskResponse>> GetAllItemsAsync()
    {
        var tasks = await taskRepository.ListAsync();
        

        return models;
    }


    public async Task<ItemModel> GetByIdAsync(int? id)
    {
        if (id is null) throw new ArgumentNullException(nameof(id));
        var model = await ItemMapper.ToModel(await itemRepository.GetByIdAsync((int)id), userRepository);
        return model;
    }

    public async Task<ICollection<ItemModel>> GetByBoardIdAsync(int boardId)
    {
        var items = await itemRepository.GetItemsByBoardIdAsync(boardId);
        var models = await Task.WhenAll(items.Select(x=>ItemMapper.ToModel(x, userRepository)));
        return models;
    }

    public async Task<int> SetItemArchieved(int itemId, CancellationToken token)
    {
        var item = await itemRepository.GetByIdAsync(itemId);
        item.IsArchived = true;
        await itemRepository.UpdateAsync(item);
        return await UpdateAsync(await ItemMapper.ToModel(item, userRepository), token,
            $"Задача {item.Title} перенесена в архив", 
            "false", "true", "Archived");
    }

    public async Task<int> SetItemNotArchived(int itemId, CancellationToken token)
    {
        var item = await itemRepository.GetByIdAsync(itemId);
        item.IsArchived = false;

        return await UpdateAsync(await ItemMapper.ToModel(item, userRepository), token,
            $"Задача {item.Title} перенесена из архива в активное пользование", 
            "true", "false", "Archived");
    }

    public async Task<int> UpdateAsync(ItemModel item, CancellationToken token, string message, string oldValue, string newValue, string fieldName,
        TaskEventType eventType = TaskEventType.Updated)
    {
        await validatorManager.ValidateItemModelAsync(item);
        var entity = ItemMapper.ItemToEntity(item);
        entity.Id = item.Id;
        var updatedAt = DateTime.UtcNow;
        entity.UpdatedAt = updatedAt;
        await itemRepository.UpdateAsync(entity);
        await kafkaProducer.ProduceAsync(new TaskEventMessage
        {
            EventType = eventType,
            UserItems = item.UserItems,
            Message = message,
        }, token);
        var model = new SharedLibrary.Models.AnalyticModels.TaskHistoryModel
        {
            FieldName = fieldName,
            OldValue = oldValue,
            NewValue = newValue,
            ItemId = item.Id,
            UserId = (int)auth.GetCurrentUserId()!,
            ChangedAt = updatedAt
        };
        await httpClient.PostAsJsonAsync("create", model, cancellationToken: token);
        return entity.Id;
    }

    public async Task<int> AddUserToItemAsync(int newUserId, int itemId, CancellationToken cancellationToken)
    {
        var item = await itemRepository.GetByIdAsync(itemId);
        await validatorManager.ValidateAddUserToItemAsync((int)item.ProjectId!, newUserId, itemId);
        var itemUserEntity = new UserItemEntity
        {
            ItemId = itemId,
            UserId = newUserId
        };

        var oldValue = new List<UserItemEntity>(item.UserItems);
        await itemRepository.AddUserToItemAsync(itemUserEntity);
        item.UserItems.Add(itemUserEntity);
        await UpdateAsync(await ItemMapper.ToModel(item, userRepository), cancellationToken, 
            $"В {item.Title} добавлен пользователь с айди {newUserId}", oldValue.ToString(), item.UserItems.ToString(), 
            "UserItems", TaskEventType.AddedUser); //пока оставил список юзеров через тустринг, потому решу как правильно передавать старое и новое значение
        //TODO ПРИДУМАТЬ!!!
        return itemUserEntity.Id;
    }

    public async Task<ICollection<ItemModel>> GetArchievedItemsInProject(int projectId)
    {
        await validatorManager.ValidateUserInProjectAsync(projectId);
        var items = await itemRepository.GetItemsByProjectIdAsync(projectId);
        var models = await Task.WhenAll(items.Where(x=>x.IsArchived).Select(x=>ItemMapper.ToModel(x, userRepository)));

        return models;
    }

    public async Task<ICollection<ItemModel>> GetArchievedItemsInBoard(int boardId)
    {
        var projectId = (await projectRepository.GetByBoardIdAsync(boardId)).Id;
        await validatorManager.ValidateUserInProjectAsync(projectId);
        var items = await itemRepository.GetItemsByBoardIdAsync(boardId);
        var models = await Task.WhenAll(items.Where(x => x.IsArchived).Select(x=> ItemMapper.ToModel(x, userRepository)));

        return models;
    }

    public async Task<ICollection<ItemModel>> GetBugsItemsInProject(int projectId)
    {
        await validatorManager.ValidateUserInProjectAsync(projectId);
        var items = await itemRepository.GetItemsByProjectIdAsync(projectId);
        var models = await Task.WhenAll(items.Where(x => x.ItemTypeId == ItemType.BUG).Select(x => ItemMapper.ToModel(x, userRepository)));

        return models;
    }

    public async Task<ICollection<ItemModel>> GetBugsItemsInBoard(int boardId)
    {
        var projectId = (await projectRepository.GetByBoardIdAsync(boardId)).Id;
        await validatorManager.ValidateUserInProjectAsync(projectId);
        var items = await itemRepository.GetItemsByBoardIdAsync(boardId);
        var models = await Task.WhenAll(items.Where(x => x.ItemTypeId == ItemType.BUG).Select(x => ItemMapper.ToModel(x, userRepository)));

        return models;
    }

    public async Task<ICollection<ItemModel>> GetByProjectIdAsync(int projectId)
    {
        await validatorManager.ValidateUserInProjectAsync(projectId);
        var items = await itemRepository.GetItemsByProjectIdAsync(projectId);
        var models = await Task.WhenAll(items.Select(x=> ItemMapper.ToModel(x, userRepository)));
        return models;
    }

    public async Task<ItemModel> GetByTitle(string title)
    {
        return await ItemMapper.ToModel(await itemRepository.GetByNameAsync(title), userRepository);
    }


    public async Task<ICollection<ItemModel>> GetCurrentUserItems()
    {
        var userId = auth.GetCurrentUserId();
        if (userId is null || userId == -1) throw new NotAuthorizedException();
        var items = await itemRepository.GetCurrentUserItemsAsync((int)userId);
        var models = await Task.WhenAll(items.Select(x=> ItemMapper.ToModel(x, userRepository)));

        return models;
    }

    public async Task<ICollection<ItemModel>> GetItemsByUserId(int userId, int projectId)
    {
        await validatorManager.ValidateAddUserToItemAsync(projectId, userId);
        var items = await itemRepository.GetItemsByUserIdAsync(userId, projectId);
        var models = await Task.WhenAll(items.Select(x=> ItemMapper.ToModel(x, userRepository)));

        return models;
    }

    public async Task<int> AddCommentToItemAsync(CommentModel commentModel, IFormFile? attachment)
    {
        var userId = auth.GetCurrentUserId();
        if (userId is null || userId == -1) throw new NotAuthorizedException();
        
        var item = await itemRepository.GetByIdAsync(commentModel.ItemId);
        if (item is null) throw new ItemNotFoundException();

        await validatorManager.ValidateUserInProjectAsync(item.ProjectId);
        var commentEntity = CommentMapper.ToEntity(commentModel);

        commentEntity.AuthorId = (int)userId;

        await commentRepository.CreateAsync(commentEntity);

        if (attachment is not null)
        {
            var docPath = Environment.GetEnvironmentVariable("ATTACHMENT_STORAGE_PATH");

            if (string.IsNullOrEmpty(docPath))
                throw new ArgumentNullException("Переменная окружения ATTACHMENT_STORAGE_PATH не задана");

            Directory.CreateDirectory(docPath);

            var uniqueFileName = $"{Guid.NewGuid()}_{attachment.FileName}";

            var filePath = Path.Combine(docPath, uniqueFileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await attachment.CopyToAsync(stream);
            }

            docPath = $"/{uniqueFileName}";

            var attachmentEntity = new AttachmentEntity 
            { 
                AuthorId = (int)userId ,
                UploadedAt = DateTime.UtcNow,
                CommentId = commentEntity.Id,
                FilePath = docPath,
            };

            await attachmentRepository.CreateAsync(attachmentEntity);

           
        }

        var model = new SharedLibrary.Models.AnalyticModels.TaskHistoryModel
        {
            FieldName = "Комментарий",
            OldValue = "",
            NewValue = commentModel.Text,
            ItemId = item.Id,
            UserId = (int)auth.GetCurrentUserId()!,
            ChangedAt = DateTime.UtcNow
        };

        await httpClient.PostAsJsonAsync("create", model);

        return commentEntity.Id;
    }

    public async Task<ICollection<CommentModel>> GetComments(int itemId)
    {
        var userId = auth.GetCurrentUserId();
        if (userId is null || userId == -1) throw new NotAuthorizedException();

        var item = await itemRepository.GetByIdAsync(itemId);
        if (item is null) throw new ItemNotFoundException();

        await validatorManager.ValidateUserInProjectAsync(item.ProjectId);

        var comments = commentRepository.GetByItemId(itemId);

        var commentsModels = await Task.WhenAll(
            comments.Select(c => CommentMapper.ToModel(c, userRepository))
        );

        return commentsModels.ToList();
    }
}