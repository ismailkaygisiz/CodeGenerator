using System.Text.RegularExpressions;

namespace CodeGenerator
{
    public class Generator
    {
        public List<string> DetectFiles(string directory, string extension)
        {
            List<string> allfiles = Directory
                .GetFiles(Environment.CurrentDirectory, "*.*", SearchOption.AllDirectories)
                .Where(e => directory != string.Empty ? e.Contains(directory) : true && e.EndsWith(extension))
                .ToList();

            return allfiles;
        }

        public List<string> DetectObjects(string directory, string extension)
        {
            List<string> objects = new List<string>();
            foreach (string file in DetectFiles(directory, extension))
            {
                string fileContent = FileHelper.ReadFile(file);
                const string pattern = @"(((internal)|(public)|(private)|(protected)|(sealed)|(abstract)|(static))?[\s\r\n\t]+){0,2}class[\s\S]+?(?={)";
                var matches = Regex.Matches(fileContent, pattern, RegexOptions.Multiline);
                objects.AddRange(matches.Cast<Match>().Select(x => x.Value.Trim().Split(" ")[2]));
            }

            return objects;
        }

        public void Generate(string className, string type, string projectName, string id)
        {
            switch (type)
            {
                case "Repository":
                    GenerateRepository(className, projectName);
                    break;
                case "Service":
                    GenerateService(className, projectName, id);
                    break;
                case "Feature":
                    GenerateFeature(className, projectName, id);
                    GenerateController(className, projectName, id);
                    break;
                case "RepositoryWithService":
                    GenerateRepository(className, projectName);
                    GenerateService(className, projectName, id);
                    break;
                case "RepositoryWithFeature":
                    GenerateRepository(className, projectName);
                    GenerateFeature(className, projectName, id);
                    GenerateController(className, projectName, id);
                    break;
                case "All":
                    GenerateRepository(className, projectName);
                    GenerateService(className, projectName, id);
                    GenerateFeature(className, projectName, id);
                    GenerateController(className, projectName, id);
                    break;
            }
        }

        public void GenerateRepository(string className, string projectName)
        {
            var applicationNameSpace = projectName + ".Application.Repositories";
            var persistenceNameSpace = projectName + ".Persistence.Repositories";

            var applicationText = $@"using Core.Persistence.Repositories;
using {projectName}.Domain.Entities;

namespace {applicationNameSpace}
{{
    public interface I{className}Repository : IRepository<{className}>, IAsyncRepository<{className}>
    {{
    }}
}}
";

            var persistenceText = $@"using Core.Persistence.Repositories;
using {applicationNameSpace};
using {projectName}.Domain.Entities;
using {projectName}.Persistence.Contexts;

namespace {persistenceNameSpace}
{{
    public class {className}Repository : EfRepositoryBase<{className}, ProjectDbContext>, I{className}Repository
    {{
        public {className}Repository(ProjectDbContext context) : base(context)
        {{
        }}
    }}
}}
";

            var applicationDirectory = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.Contains(projectName + ".Application\\Repositories"))
                .First();

            var persistenceDirectory = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.Contains(projectName + ".Persistence\\Repositories"))
                .First();

            FileHelper.CreateAndWriteFile($"{applicationDirectory + "\\I" + className + "Repository.cs"}", applicationText);
            FileHelper.CreateAndWriteFile($"{persistenceDirectory + "\\" + className + "Repository.cs"}", persistenceText);
        }

        public void GenerateService(string className, string projectName, string id)
        {
            var applicationNameSpace = projectName + ".Application.Services";
            var persistenceNameSpace = projectName + ".Persistence.Services";
            var repositoryName = $"_{char.ToLower(className[0]) + className.Substring(1)}Repository";

            var applicationText = $@"using {projectName}.Domain.Entities;

namespace {applicationNameSpace}
{{
    public interface I{className}Service
    {{
        Task<{className}> Add({className} entity);
        Task<{className}> Delete({className} entity);
        Task<{className}> Update({className} entity);
        Task<{className}?> GetById({id} id);
        Task<List<{className}>> GetList();
    }}
}}
";

            var persistenceText = $@"using {projectName}.Application.Repositories;
using {applicationNameSpace};
using {projectName}.Domain.Entities;

namespace {persistenceNameSpace}
{{
    public class {className}Service : I{className}Service
    {{
        private readonly I{className}Repository {repositoryName};

        public {className}Service(I{className}Repository {repositoryName.Substring(1)})
        {{
            {repositoryName} = {repositoryName.Substring(1)};
        }}

        public async Task<{className}> Add({className} entity)
        {{
            entity.CreatedDate = DateTime.Now;
            await {repositoryName}.AddAsync(entity);
            return entity;
        }}

        public async Task<{className}> Delete({className} entity)
        {{
            await {repositoryName}.DeleteAsync(entity, true);
            return entity;
        }}

        public async Task<{className}> Update({className} entity)
        {{
            entity.UpdatedDate = DateTime.Now;
            await {repositoryName}.UpdateAsync(entity);
            return entity;
        }}

        public async Task<{className}?> GetById({id} id)
        {{
            return await {repositoryName}.GetAsync(e => e.Id == id);
        }}

        public async Task<List<{className}>> GetList()
        {{
            return (await {repositoryName}.GetListAsync()).ToList();
        }}
    }}
}}
";
            var applicationDirectory = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.Contains(projectName + ".Application\\Services"))
                .First();

            var persistenceDirectory = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.Contains(projectName + ".Persistence\\Services"))
                .First();

            FileHelper.CreateAndWriteFile($"{applicationDirectory + "\\I" + className + "Service.cs"}", applicationText);
            FileHelper.CreateAndWriteFile($"{persistenceDirectory + "\\" + className + "Service.cs"}", persistenceText);
        }

        public void GenerateFeature(string className, string projectName, string id)
        {
            var featureDirectory = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.EndsWith(projectName + ".Application\\Features"))
                .First();

            Console.Write("Please input a name for feature folder >> ");
            var featureSubDirectory = Console.ReadLine();
            var features = featureSubDirectory;
            featureSubDirectory = featureDirectory + "\\" + featureSubDirectory;

            var featuresNameSpace = projectName + ".Application.Features";
            var applicationRepositoryNameSpace = projectName + ".Application.Repositories";
            var repositoryName = $"_{char.ToLower(className[0]) + className.Substring(1)}Repository";

            var createCommandText = $@"using MediatR;

namespace {featuresNameSpace}.{features}.Commands.Create
{{
    public class Create{className}Command : IRequest<Created{className}Response>
    {{
    }}
}}
";

            var createdResponseText = $@"namespace {featuresNameSpace}.{features}.Commands.Create
{{
    public class Created{className}Response
    {{
        
    }}
}}
";

            var createCommandHandlerText = $@"using AutoMapper;
using Core.CrossCuttingConcerns.Exceptions.ExceptionTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using {applicationRepositoryNameSpace};
using {projectName}.Domain.Entities;
using {featuresNameSpace}.{features}.Rules;

namespace {featuresNameSpace}.{features}.Commands.Create
{{
    public class Create{className}CommandHandler : IRequestHandler<Create{className}Command, Created{className}Response>
    {{
        private readonly I{className}Repository {repositoryName};
        private readonly IMapper _mapper;
        private readonly {className}BusinessRules _rules;
        
        public Create{className}CommandHandler(I{className}Repository {repositoryName.Substring(1)}, IMapper mapper, {className}BusinessRules rules)
        {{
            {repositoryName} = {repositoryName.Substring(1)};
            _mapper = mapper;
            _rules = rules;
        }}

        public async Task<Created{className}Response> Handle(Create{className}Command request, CancellationToken cancellationToken)
        {{
            {className} entity = _mapper.Map<{className}>(request);
            entity.CreatedDate = DateTime.Now;

            await {repositoryName}.AddAsync(entity);

            Created{className}Response response = _mapper.Map<Created{className}Response>(entity);
            return response;
        }}
    }}
}}
";


            var deleteCommandText = $@"using MediatR;

namespace {featuresNameSpace}.{features}.Commands.Delete
{{
    public class Delete{className}Command : IRequest<Deleted{className}Response>
    {{
        public {id} Id {{ get; set; }}
    }}
}}
";

            var deletedResponseText = $@"namespace {featuresNameSpace}.{features}.Commands.Delete
{{
    public class Deleted{className}Response
    {{
        
    }}
}}
";

            var deleteCommandHandlerText = $@"using AutoMapper;
using Core.CrossCuttingConcerns.Exceptions.ExceptionTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using {applicationRepositoryNameSpace};
using {projectName}.Domain.Entities;
using {featuresNameSpace}.{features}.Rules;

namespace {featuresNameSpace}.{features}.Commands.Delete
{{
    public class Delete{className}CommandHandler : IRequestHandler<Delete{className}Command, Deleted{className}Response>
    {{
        private readonly I{className}Repository {repositoryName};
        private readonly IMapper _mapper;
        private readonly {className}BusinessRules _rules;
        
        public Delete{className}CommandHandler(I{className}Repository {repositoryName.Substring(1)}, IMapper mapper, {className}BusinessRules rules)
        {{
            {repositoryName} = {repositoryName.Substring(1)};
            _mapper = mapper;
            _rules = rules;
        }}

        public async Task<Deleted{className}Response> Handle(Delete{className}Command request, CancellationToken cancellationToken)
        {{
            {className}? entity = await {repositoryName}.GetAsync(e => e.Id == request.Id, cancellationToken: cancellationToken);
            if(entity is null)
                throw new NotFoundException(""Entity not found"");

            await {repositoryName}.DeleteAsync(entity);

            Deleted{className}Response response = _mapper.Map<Deleted{className}Response>(entity);
            return response;
        }}
    }}
}}
";


            var updateCommandText = $@"using MediatR;

namespace {featuresNameSpace}.{features}.Commands.Update
{{
    public class Update{className}Command : IRequest<Updated{className}Response>
    {{
        public {id} Id {{ get; set; }}
    }}
}}
";

            var updatedResponseText = $@"namespace {featuresNameSpace}.{features}.Commands.Update
{{
    public class Updated{className}Response
    {{
        
    }}
}}
";

            var updateCommandHandlerText = $@"using AutoMapper;
using Core.CrossCuttingConcerns.Exceptions.ExceptionTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using {applicationRepositoryNameSpace};
using {projectName}.Domain.Entities;
using {featuresNameSpace}.{features}.Rules;

namespace {featuresNameSpace}.{features}.Commands.Update
{{
    public class Update{className}CommandHandler : IRequestHandler<Update{className}Command, Updated{className}Response>
    {{
        private readonly I{className}Repository {repositoryName};
        private readonly IMapper _mapper;
        private readonly {className}BusinessRules _rules;
        
        public Update{className}CommandHandler(I{className}Repository {repositoryName.Substring(1)}, IMapper mapper, {className}BusinessRules rules)
        {{
            {repositoryName} = {repositoryName.Substring(1)};
            _mapper = mapper;
            _rules = rules;
        }}

        public async Task<Updated{className}Response> Handle(Update{className}Command request, CancellationToken cancellationToken)
        {{
            {className}? entity = await {repositoryName}.GetAsync(e => e.Id == request.Id, cancellationToken: cancellationToken);
            if(entity is null)
                throw new NotFoundException(""Entity not found"");

            entity.UpdatedDate = DateTime.Now;

            await {repositoryName}.SaveChangesAsync();

            Updated{className}Response response = _mapper.Map<Updated{className}Response>(entity);
            return response;
        }}
    }}
}}
";

            var getByIdQueryText = $@"using MediatR;

namespace {featuresNameSpace}.{features}.Queries.GetById
{{
    public class GetById{className}Query : IRequest<GetById{className}Response>
    {{
        public {id} Id {{ get; set; }}
    }}
}}
";

            var getByIdResponseText = $@"namespace {featuresNameSpace}.{features}.Queries.GetById
{{
    public class GetById{className}Response
    {{
        
    }}
}}
";

            var getByIdQueryHandlerText = $@"using AutoMapper;
using Core.CrossCuttingConcerns.Exceptions.ExceptionTypes;
using MediatR;
using Microsoft.EntityFrameworkCore;
using {applicationRepositoryNameSpace};
using {projectName}.Domain.Entities;
using {featuresNameSpace}.{features}.Rules;

namespace {featuresNameSpace}.{features}.Queries.GetById
{{
    public class GetById{className}QueryHandler : IRequestHandler<GetById{className}Query, GetById{className}Response>
    {{
        private readonly I{className}Repository {repositoryName};
        private readonly IMapper _mapper;
        private readonly {className}BusinessRules _rules;
        
        public GetById{className}QueryHandler(I{className}Repository {repositoryName.Substring(1)}, IMapper mapper, {className}BusinessRules rules)
        {{
            {repositoryName} = {repositoryName.Substring(1)};
            _mapper = mapper;
            _rules = rules;
        }}

        public async Task<GetById{className}Response> Handle(GetById{className}Query request, CancellationToken cancellationToken)
        {{
            {className}? entity = await {repositoryName}.GetAsync(e => e.Id == request.Id, cancellationToken: cancellationToken);
            if(entity is null)
                throw new NotFoundException(""Entity not found"");

            GetById{className}Response response = _mapper.Map<GetById{className}Response>(entity);
            return response;
        }}
    }}
}}
";

            var getListQueryText = $@"using Core.Application.Requests;
using Core.Persistence.Pagination;
using MediatR;

namespace {featuresNameSpace}.{features}.Queries.GetList
{{
    public class GetList{className}Query : IRequest<Paginate<GetList{className}Response>>
    {{
        public PageRequest PageRequest {{ get; set; }} = new();
    }}
}}
";

            var getListResponseText = $@"namespace {featuresNameSpace}.{features}.Queries.GetList
{{
    public class GetList{className}Response
    {{
        
    }}
}}
";

            var getListQueryHandlerText = $@"using AutoMapper;
using Core.CrossCuttingConcerns.Exceptions.ExceptionTypes;
using Core.Persistence.Pagination;
using MediatR;
using Microsoft.EntityFrameworkCore;
using {applicationRepositoryNameSpace};
using {projectName}.Domain.Entities;
using {featuresNameSpace}.{features}.Rules;

namespace {featuresNameSpace}.{features}.Queries.GetList
{{
    public class GetList{className}QueryHandler : IRequestHandler<GetList{className}Query, Paginate<GetList{className}Response>>
    {{
        private readonly I{className}Repository {repositoryName};
        private readonly IMapper _mapper;
        private readonly {className}BusinessRules _rules;
        
        public GetList{className}QueryHandler(I{className}Repository {repositoryName.Substring(1)}, IMapper mapper, {className}BusinessRules rules)
        {{
            {repositoryName} = {repositoryName.Substring(1)};
            _mapper = mapper;
            _rules = rules;
        }}

        public async Task<Paginate<GetList{className}Response>> Handle(GetList{className}Query request, CancellationToken cancellationToken)
        {{
            IPaginate<{className}> entities = await {repositoryName}.GetListPagedAsync(
                index: request.PageRequest.Index,
                size: request.PageRequest.Size,
                cancellationToken: cancellationToken
            );

            Paginate<GetList{className}Response> response = _mapper.Map<Paginate<GetList{className}Response>>(entities);
            return response;
        }}
    }}
}}
";

            var mappingProfilesText = $@"using AutoMapper;
using Core.Persistence.Pagination;
using {featuresNameSpace}.{features}.Commands.Create;
using {featuresNameSpace}.{features}.Commands.Update;
using {featuresNameSpace}.{features}.Commands.Delete;
using {featuresNameSpace}.{features}.Queries.GetById;
using {featuresNameSpace}.{features}.Queries.GetList;
using {projectName}.Domain.Entities;

namespace {featuresNameSpace}.{features}.Profiles
{{
    public class MappingProfiles : Profile
    {{
        public MappingProfiles()
        {{
            CreateMap<{className}, Create{className}Command>().ReverseMap();
            CreateMap<{className}, Created{className}Response>().ReverseMap();

            CreateMap<{className}, Deleted{className}Response>().ReverseMap();

            CreateMap<{className}, Update{className}Command>().ReverseMap();
            CreateMap<{className}, Updated{className}Response>().ReverseMap();

            CreateMap<{className}, GetById{className}Response>().ReverseMap();

            CreateMap<{className}, GetList{className}Response>().ReverseMap();
            CreateMap<Paginate<{className}>, Paginate<GetList{className}Response>>().ReverseMap();
        }}
    }}
}}
";

            var businessRulesText = $@"using Core.Application.Rules;
using Core.CrossCuttingConcerns.Exceptions.ExceptionTypes;
using {projectName}.Application.Repositories;

namespace {featuresNameSpace}.{features}.Rules
{{
    public class {className}BusinessRules : BaseBusinessRules
    {{
        private readonly I{className}Repository {repositoryName};

        public {className}BusinessRules(I{className}Repository {repositoryName.Substring(1)})
        {{
            {repositoryName} = {repositoryName.Substring(1)};
        }}
    }}
}}
";

            Directory.CreateDirectory(featureDirectory + $"\\{features}");
            Directory.CreateDirectory(featureSubDirectory + "\\Commands\\Create");
            Directory.CreateDirectory(featureSubDirectory + "\\Commands\\Delete");
            Directory.CreateDirectory(featureSubDirectory + "\\Commands\\Update");
            Directory.CreateDirectory(featureSubDirectory + "\\Queries\\GetById");
            Directory.CreateDirectory(featureSubDirectory + "\\Queries\\GetList");

            Directory.CreateDirectory(featureSubDirectory + "\\Profiles");
            Directory.CreateDirectory(featureSubDirectory + "\\Rules");

            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Create\\" + "Create" + className + "Command.cs"}", createCommandText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Create\\" + "Create" + className + "CommandHandler.cs"}", createCommandHandlerText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Create\\" + "Created" + className + "Response.cs"}", createdResponseText);

            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Delete\\" + "Delete" + className + "Command.cs"}", deleteCommandText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Delete\\" + "Delete" + className + "CommandHandler.cs"}", deleteCommandHandlerText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Delete\\" + "Deleted" + className + "Response.cs"}", deletedResponseText);

            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Update\\" + "Update" + className + "Command.cs"}", updateCommandText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Update\\" + "Update" + className + "CommandHandler.cs"}", updateCommandHandlerText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Commands\\Update\\" + "Updated" + className + "Response.cs"}", updatedResponseText);

            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Queries\\GetById\\" + "GetById" + className + "Query.cs"}", getByIdQueryText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Queries\\GetById\\" + "GetById" + className + "QueryHandler.cs"}", getByIdQueryHandlerText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Queries\\GetById\\" + "GetById" + className + "Response.cs"}", getByIdResponseText);

            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Queries\\GetList\\" + "GetList" + className + "Query.cs"}", getListQueryText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Queries\\GetList\\" + "GetList" + className + "QueryHandler.cs"}", getListQueryHandlerText);
            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Queries\\GetList\\" + "GetList" + className + "Response.cs"}", getListResponseText);

            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Profiles\\" + "MappingProfiles.cs"}", mappingProfilesText);

            FileHelper.CreateAndWriteFile($"{featureSubDirectory + "\\Rules\\" + className + "BusinessRules.cs"}", businessRulesText);
        }

        public void GenerateController(string className, string projectName, string id)
        {
            var controllersDirectory = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.EndsWith(projectName + ".WebAPI\\Controllers"))
                .First();

            Console.Write("Please input a name for controller file >> ");
            var controllerName = Console.ReadLine();
            var controllerFilePath = controllersDirectory + "\\" + controllerName + "Controller.cs";

            var dtosDriectory = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.EndsWith(projectName + ".WebAPI\\Dtos"))
                .First();
            var dtoFilePath = dtosDriectory + $"\\{className}";

            var profiles = Directory
                .GetDirectories(Environment.CurrentDirectory, "*", SearchOption.AllDirectories)
                .Where(e => e.EndsWith(projectName + ".WebAPI\\Profiles"))
                .First();
            var profileFilePath = profiles + $"\\{className}MappingProfiles.cs";

            var controllerText = $@"using Core.API.Controllers;
using Core.Persistence.Pagination;
using Microsoft.AspNetCore.Mvc;
using {projectName}.Application.Features.{controllerName}.Commands.Create;
using {projectName}.Application.Features.{controllerName}.Commands.Delete;
using {projectName}.Application.Features.{controllerName}.Commands.Update;
using {projectName}.Application.Features.{controllerName}.Queries.GetById;
using {projectName}.Application.Features.{controllerName}.Queries.GetList;
using {projectName}.WebAPI.Dtos.{className};

namespace {projectName}.WebAPI.Controllers
{{
    [Route(""api/[controller]/[action]"")]
    [ApiController]
    public class {controllerName}Controller : BaseController
    {{
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Create{className}Dto request)
        {{
            Created{className}Response response = await Mediator.Send(Mapper.Map<Create{className}Command>(request));
            return Ok(response);
        }}

        [HttpDelete]
        public async Task<IActionResult> Delete([FromQuery] Delete{className}Dto request)
        {{
            Deleted{className}Response response = await Mediator.Send(Mapper.Map<Delete{className}Command>(request));
            return Ok(response);
        }}

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] Update{className}Dto request)
        {{
            Updated{className}Response response = await Mediator.Send(Mapper.Map<Update{className}Command>(request));
            return Ok(response);
        }}

        [HttpGet(""{{Id}}"")]
        public async Task<IActionResult> GetById([FromRoute] GetById{className}Dto request)
        {{
            GetById{className}Response response = await Mediator.Send(Mapper.Map<GetById{className}Query>(request));
            return Ok(response);
        }}

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] GetList{className}Dto request)
        {{
            IPaginate<GetList{className}Response> response = await Mediator.Send(Mapper.Map<GetList{className}Query>(request));
            return Ok(response);
        }}
    }}
}}
";

            var createDtoText = $@"namespace {projectName}.WebAPI.Dtos.{className}
{{
    public class Create{className}Dto
    {{
    
    }}
}}
";
            var deleteDtoText = $@"namespace {projectName}.WebAPI.Dtos.{className}
{{
    public class Delete{className}Dto
    {{
        public {id} Id {{ get; set; }}
    }}
}}
";

            var updateDtoText = $@"namespace {projectName}.WebAPI.Dtos.{className}
{{
    public class Update{className}Dto
    {{
        public {id} Id {{ get; set; }}
    }}
}}
";

            var getByIdDtoText = $@"namespace {projectName}.WebAPI.Dtos.{className}
{{
    public class GetById{className}Dto
    {{
        public {id} Id {{ get; set; }}
    }}
}}
";

            var getListDtoText = $@"using Core.Application.Requests;

namespace {projectName}.WebAPI.Dtos.{className}
{{
    public class GetList{className}Dto
    {{
        public PageRequest? PageRequest {{ get; set; }}
    }}
}}
";

            var profilesText = $@"using AutoMapper;
using {projectName}.Application.Features.{controllerName}.Commands.Create;
using {projectName}.Application.Features.{controllerName}.Commands.Delete;
using {projectName}.Application.Features.{controllerName}.Commands.Update;
using {projectName}.Application.Features.{controllerName}.Queries.GetById;
using {projectName}.Application.Features.{controllerName}.Queries.GetList;
using {projectName}.WebAPI.Dtos.{className};

namespace {projectName}.WebAPI.Profiles
{{
    public class {className}MappingProfiles : Profile
    {{
        public {className}MappingProfiles()
        {{  
            CreateMap<Create{className}Dto, Create{className}Command>();
            CreateMap<Delete{className}Dto, Delete{className}Command>();
            CreateMap<Update{className}Dto, Update{className}Command>();
            CreateMap<GetById{className}Dto, GetById{className}Query>();
            CreateMap<GetList{className}Dto, GetList{className}Query>();
        }}
    }}
}}";

            FileHelper.CreateAndWriteFile(controllerFilePath, controllerText);

            Directory.CreateDirectory(dtoFilePath);
            FileHelper.CreateAndWriteFile(dtoFilePath + $"\\Create{className}Dto.cs", createDtoText);
            FileHelper.CreateAndWriteFile(dtoFilePath + $"\\Delete{className}Dto.cs", deleteDtoText);
            FileHelper.CreateAndWriteFile(dtoFilePath + $"\\Update{className}Dto.cs", updateDtoText);
            FileHelper.CreateAndWriteFile(dtoFilePath + $"\\GetById{className}Dto.cs", getByIdDtoText);
            FileHelper.CreateAndWriteFile(dtoFilePath + $"\\GetList{className}Dto.cs", getListDtoText);
            FileHelper.CreateAndWriteFile(profileFilePath, profilesText);
        }
    }
}
