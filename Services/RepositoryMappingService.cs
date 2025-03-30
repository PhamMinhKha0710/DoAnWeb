using DoAnWeb.Models;
using DoAnWeb.Repositories;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DoAnWeb.Services
{
    public class RepositoryMappingService : IRepositoryMappingService
    {
        private readonly IRepositoryMappingRepository _repositoryMappingRepository;
        private readonly ILogger<RepositoryMappingService> _logger;

        public RepositoryMappingService(
            IRepositoryMappingRepository repositoryMappingRepository,
            ILogger<RepositoryMappingService> logger)
        {
            _repositoryMappingRepository = repositoryMappingRepository;
            _logger = logger;
        }

        public void CreateMapping(int devCommunityRepositoryId, int giteaRepositoryId, string htmlUrl, string cloneUrl, string sshUrl)
        {
            try
            {
                var mapping = new RepositoryMapping
                {
                    DevCommunityRepositoryId = devCommunityRepositoryId,
                    GiteaRepositoryId = giteaRepositoryId,
                    HtmlUrl = htmlUrl,
                    CloneUrl = cloneUrl,
                    SshUrl = sshUrl,
                    CreatedAt = DateTime.Now
                };

                _repositoryMappingRepository.Add(mapping);
                _logger.LogInformation($"Created repository mapping: DevCommunity ID {devCommunityRepositoryId} -> Gitea ID {giteaRepositoryId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating repository mapping for DevCommunity ID {devCommunityRepositoryId} and Gitea ID {giteaRepositoryId}");
                throw;
            }
        }

        public void DeleteMapping(int mappingId)
        {
            try
            {
                _repositoryMappingRepository.Delete(mappingId);
                _logger.LogInformation($"Deleted repository mapping with ID {mappingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting repository mapping with ID {mappingId}");
                throw;
            }
        }

        public RepositoryMapping GetByDevCommunityId(int repositoryId)
        {
            try
            {
                return _repositoryMappingRepository.GetByDevCommunityId(repositoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repository mapping for DevCommunity ID {repositoryId}");
                throw;
            }
        }

        public RepositoryMapping GetByGiteaId(int giteaRepositoryId)
        {
            try
            {
                return _repositoryMappingRepository.GetByGiteaId(giteaRepositoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repository mapping for Gitea ID {giteaRepositoryId}");
                throw;
            }
        }

        public IEnumerable<RepositoryMapping> GetMappingsByUserId(int userId)
        {
            try
            {
                return _repositoryMappingRepository.GetMappingsByUserId(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting repository mappings for user ID {userId}");
                throw;
            }
        }

        public void UpdateMapping(RepositoryMapping mapping)
        {
            try
            {
                _repositoryMappingRepository.Update(mapping);
                _logger.LogInformation($"Updated repository mapping with ID {mapping.MappingId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating repository mapping with ID {mapping.MappingId}");
                throw;
            }
        }
    }
} 