// Type definitions
export interface FileItemDto {
  id: string;
  path: string;
  hashHex: string;
  size: number;
  mimeType: string;
  tags: string[];
  createdDate: string;
  isPhoto: boolean;
  photoDateTaken?: string;
  cameraMake?: string;
  cameraModel?: string;
  latitude?: number;
  longitude?: number;
  thumbnailPath?: string;
  folderId?: string;
}

export interface FolderDto {
  id: string;
  path: string;
  name: string;
  parentFolderId?: string;
  createdDate: string;
  subFolders?: FolderDto[];
}

export interface SearchFilesResult {
  files: FileItemDto[];
  totalCount: number;
}

export interface GetFoldersResult {
  folders: FolderDto[];
}
