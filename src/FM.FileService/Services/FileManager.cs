﻿using FM.FileService.Data;
using FM.FileService.Data.Specification.Interfaces;
using FM.FileService.Domain.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using FM.FileService.Enums;
using FM.FileService.Data.Specification.FileSpecification;
using FM.Common.Extensions;
using FM.Common.DataAccess.Interfaces;
using FM.FileService.Filters;
using FM.FileService.Domain.DTOs;

namespace FM.FileService.Services
{
    public class FileManager
    {
        private IWebHostEnvironment _appEnvironment;
        private readonly FileDbContext _context;

        public FileManager(IWebHostEnvironment appEnvironment, FileDbContext context)
        {
            _appEnvironment = appEnvironment;
            _context = context;
        }

        public async Task<FileUploadResult> AddFileAsync(List<IFormFile> uploadFiles, string directoryPath)
        {
            long size = uploadFiles.Sum(f => f.Length) / 1024;
            string filePath;

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            foreach (var fileToUpload in uploadFiles)
            {
                filePath = string.Format("{0}{1}{2}", directoryPath, Path.DirectorySeparatorChar, fileToUpload.FileName);

                if (File.Exists(filePath))
                {
                    return new FileUploadResult
                    {
                        IsSuccess = false,
                        Message = $"File {fileToUpload.FileName} has already exists."
                    };
                }

                using (var fileStream = new FileStream(_appEnvironment.WebRootPath + filePath, FileMode.Create))
                {
                    await fileToUpload.CopyToAsync(fileStream);
                }
            }
            return new FileUploadResult
            {
                IsSuccess = true,
                Count = uploadFiles.Count,
                Size = size
            };
        }

        public async Task<IReadOnlyList<FileEntity>> GetFilesAsync(FileFilterDto fileFilterDto)
        {
            Expression<Func<FileEntity, object>> sortingColumnExp = null;
            Expression<Func<FileEntity, bool>> criterias = f => true;

            if (fileFilterDto.Filters != null)
            {
                if (fileFilterDto.Filters.Id != null)
                {
                    criterias = criterias.AndAlso(f => f.Id.ToString().Contains(fileFilterDto.Filters.Id));
                }
                if (fileFilterDto.Filters.Name != null)
                {
                    criterias = criterias.AndAlso(f => f.Name.Contains(fileFilterDto.Filters.Name));
                }
                if (fileFilterDto.Filters.Size != null)
                {
                    criterias = criterias.AndAlso(f => f.Size >= fileFilterDto.Filters.Size[0] 
                        && f.Size <= fileFilterDto.Filters.Size[1]);
                }
                if (fileFilterDto.Filters.UploadTime != null)
                {
                    criterias = criterias.AndAlso(f => f.UploadedTime >= fileFilterDto.Filters.UploadTime[0].ConvertToUtc()
                            && f.UploadedTime <= (fileFilterDto.Filters.UploadTime[1] + 1).ConvertToUtc());
                }
                if (fileFilterDto.Filters.AllowedAnonymous != null)
                {
                    criterias = criterias.AndAlso(f => f.AllowedAnonymous == fileFilterDto.Filters.AllowedAnonymous);
                }
            }

            FileFilterSpecification<FileEntity> fileFilterSpecification = new FileFilterSpecification<FileEntity>(criterias, 
            fileFilterDto.ItemsPage * fileFilterDto.PageIndex, 
            fileFilterDto.ItemsPage);

            if (fileFilterDto.SortingColumn != null)
            {
                switch (fileFilterDto.SortingColumn)
                {
                    case "Id":
                        sortingColumnExp = f => f.Id;
                        break;
                    case "Name":
                        sortingColumnExp = f => f.Name;
                        break;
                    case "UploadedTime":
                        sortingColumnExp = f => f.UploadedTime;
                        break;
                    case "Size":
                        sortingColumnExp = f => f.Size;
                        break;
                    case "AllowedAnonymous":
                        sortingColumnExp = f => f.AllowedAnonymous;
                        break;
                }
            }

            if (fileFilterDto.SortingMode == FileSortingMode.OrderBy)
            {
                fileFilterSpecification.ApplyOrderBy(sortingColumnExp);
            }
            else if (fileFilterDto.SortingMode == FileSortingMode.OrderByDescending)
            {
                fileFilterSpecification.ApplyOrderByDescending(sortingColumnExp);
            }

            var result = await ApplySpecification(fileFilterSpecification).ToArrayAsync();
            return result;
        }

        public async Task<IReadOnlyList<FileReadHistoryEntity>> GetFileHistoriesAsync(Guid fileId, 
            int pageIndex,
            int itemsPage)
        {
            FileFilterSpecification<FileReadHistoryEntity> fileHistoryFilterSpecification = 
                    new FileFilterSpecification<FileReadHistoryEntity>(f => f.FileId == fileId,
                    itemsPage * pageIndex,
                    itemsPage);

            var result = await ApplySpecification(fileHistoryFilterSpecification).ToArrayAsync();
            return result;
        }

        private IQueryable<T> ApplySpecification<T>(ISpecification<T> spec) where T : class, IEntity<Guid>
        {
            return SpecificationEvaluator<T>.GetQuery(_context.Set<T>().AsQueryable(), spec);
        }
    }
}
