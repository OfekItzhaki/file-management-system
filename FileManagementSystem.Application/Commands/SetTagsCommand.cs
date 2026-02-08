using MediatR;

namespace FileManagementSystem.Application.Commands;

public record SetTagsCommand(Guid FileId, List<string> Tags) : IRequest<bool>;
