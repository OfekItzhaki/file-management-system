import type { FolderDto } from '../types';

interface FolderTreeProps {
  folders: FolderDto[];
  onFolderSelect: (folderId: string | undefined) => void;
  selectedFolderId?: string;
}

const FolderTree = ({ folders, onFolderSelect, selectedFolderId }: FolderTreeProps) => {
  const renderFolder = (folder: FolderDto, level: number = 0) => {
    const isSelected = folder.id === selectedFolderId;
    return (
      <div key={folder.id}>
        <div
          onClick={() => onFolderSelect(isSelected ? undefined : folder.id)}
          style={{
            padding: '0.5rem',
            paddingLeft: `${level * 1.5 + 0.5}rem`,
            cursor: 'pointer',
            background: isSelected ? '#e3f2fd' : 'transparent',
            borderRadius: '4px',
            marginBottom: '2px',
          }}
        >
          {folder.name}
        </div>
        {folder.subFolders?.map((subFolder) => renderFolder(subFolder, level + 1))}
      </div>
    );
  };

  return (
    <div>
      <h3>Folders</h3>
      <div
        onClick={() => onFolderSelect(undefined)}
        style={{
          padding: '0.5rem',
          cursor: 'pointer',
          background: selectedFolderId === undefined ? '#e3f2fd' : 'transparent',
          borderRadius: '4px',
          marginBottom: '8px',
          fontWeight: 'bold',
        }}
      >
        All Files
      </div>
      {folders.map((folder) => renderFolder(folder))}
    </div>
  );
};

export default FolderTree;
