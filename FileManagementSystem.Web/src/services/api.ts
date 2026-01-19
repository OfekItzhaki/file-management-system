import axios from 'axios';
import type { FileItemDto, FolderDto, SearchFilesResult, GetFoldersResult } from '../types';

// Use proxy in dev, or direct URL in production
const API_BASE_URL = import.meta.env.VITE_API_URL || (import.meta.env.DEV ? '/api' : 'http://localhost:5295/api');

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Re-export types for convenience
export type { FileItemDto, FolderDto, SearchFilesResult, GetFoldersResult };

// API methods
export const fileApi = {
  getFiles: async (params?: {
    searchTerm?: string;
    tags?: string[];
    isPhoto?: boolean;
    folderId?: string;
    skip?: number;
    take?: number;
  }): Promise<SearchFilesResult> => {
    const response = await apiClient.get<SearchFilesResult>('/files', { params });
    return response.data;
  },

  getFile: async (id: string): Promise<FileItemDto> => {
    const response = await apiClient.get<FileItemDto>(`/files/${id}`);
    return response.data;
  },

  uploadFile: async (file: File, destinationFolder?: string): Promise<any> => {
    const formData = new FormData();
    formData.append('file', file);
    if (destinationFolder) {
      formData.append('destinationFolder', destinationFolder);
    }
    const response = await apiClient.post('/files/upload', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  deleteFile: async (id: string, moveToRecycleBin: boolean = true): Promise<void> => {
    await apiClient.delete(`/files/${id}`, { params: { moveToRecycleBin } });
  },

  renameFile: async (id: string, newName: string): Promise<any> => {
    const response = await apiClient.put(`/files/${id}/rename`, { newName });
    return response.data;
  },

  addTags: async (id: string, tags: string[]): Promise<void> => {
    await apiClient.post(`/files/${id}/tags`, { tags });
  },
};

export const folderApi = {
  getFolders: async (): Promise<GetFoldersResult> => {
    const response = await apiClient.get<GetFoldersResult>('/folders');
    return response.data;
  },
};

export default apiClient;
