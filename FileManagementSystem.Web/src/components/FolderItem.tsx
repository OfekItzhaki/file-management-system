import { useState } from 'react';
import type { FolderDto } from '../types';

interface FolderItemProps {
    folder: FolderDto;
    level: number;
    isSelected: boolean;
    isMobile: boolean;
    onSelect: (id: string) => void;
    onRename: (id: string, newName: string) => void;
    onDelete: (folder: FolderDto) => void;
    isDeleting?: boolean;
}

export const FolderItem = ({
    folder,
    level,
    isSelected,
    isMobile,
    onSelect,
    onRename,
    onDelete,
    isDeleting
}: FolderItemProps) => {
    const [isEditing, setIsEditing] = useState(false);
    const [editingName, setEditingName] = useState(folder.name);
    const [isHovered, setIsHovered] = useState(false);

    const handleSave = () => {
        const trimmedName = (editingName ?? '').trim();
        if (trimmedName && trimmedName !== (folder.name ?? '')) {
            onRename(folder.id!, trimmedName);
        }
        setIsEditing(false);
    };

    const handleCancel = () => {
        setEditingName(folder.name ?? '');
        setIsEditing(false);
    };

    return (
        <div
            className={`folder-item ${isSelected ? 'selected' : ''}`}
            onMouseEnter={() => setIsHovered(true)}
            onMouseLeave={() => setIsHovered(false)}
            style={{ paddingLeft: level === 0 ? '1rem' : `${level * 1.5 + 0.75}rem` }}
        >
            {isEditing ? (
                <div className="create-actions" style={{ flex: 1 }}>
                    <input
                        className="folder-input"
                        style={{ margin: 0, padding: '0.4rem 0.6rem' }}
                        value={editingName}
                        onChange={(e) => setEditingName(e.target.value)}
                        onKeyDown={(e) => {
                            if (e.key === 'Enter') handleSave();
                            else if (e.key === 'Escape') handleCancel();
                        }}
                        autoFocus
                    />
                    <button className="btn-primary" style={{ padding: '0.4rem 0.6rem' }} onClick={handleSave}>✓</button>
                    <button className="btn-secondary" style={{ padding: '0.4rem 0.6rem', background: '#ef4444' }} onClick={handleCancel}>✕</button>
                </div>
            ) : (
                <>
                    <span className="folder-name" onClick={() => onSelect(folder.id!)}>
                        📁 {folder.name?.trim().toLowerCase() === 'default' && '🛡️ '}{folder.name}
                        {((folder.fileCount ?? 0) > 0 || (folder.subFolderCount ?? 0) > 0) && (
                            <span className="folder-count">
                                ({(folder.fileCount ?? 0) + (folder.subFolderCount ?? 0)})
                            </span>
                        )}
                    </span>
                    {(isHovered || isMobile) && folder.name?.trim().toLowerCase() !== 'default' && (
                        <div className="folder-actions">
                            <button
                                className="action-btn rename-btn"
                                onClick={(e) => { e.stopPropagation(); setIsEditing(true); }}
                                title="Rename"
                            >
                                ✏️
                            </button>
                            <button
                                className="action-btn delete-btn"
                                onClick={(e) => { e.stopPropagation(); onDelete(folder); }}
                                disabled={isDeleting}
                                title="Delete"
                            >
                                🗑️
                            </button>
                        </div>
                    )}
                </>
            )}
        </div>
    );
};
