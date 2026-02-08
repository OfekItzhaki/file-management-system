import { useState, useEffect } from 'react';
import { X, Plus, Save, Loader2 } from 'lucide-react';
import { fileApi } from '../services/api';
import { toast } from 'react-hot-toast';

interface TagEditorProps {
    fileId: string;
    initialTags: string[];
    isOpen: boolean;
    onClose: () => void;
    onTagsUpdated: (newTags: string[]) => void;
}

export default function TagEditor({ fileId, initialTags, isOpen, onClose, onTagsUpdated }: TagEditorProps) {
    const [tags, setTags] = useState<string[]>(initialTags);
    const [newTag, setNewTag] = useState('');
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        setTags(initialTags);
    }, [initialTags, isOpen]);

    if (!isOpen) return null;

    const handleAddTag = (e?: React.FormEvent) => {
        e?.preventDefault();
        const trimmed = newTag.trim();
        if (trimmed && !tags.includes(trimmed)) {
            setTags([...tags, trimmed]);
            setNewTag('');
        }
    };

    const handleRemoveTag = (tagToRemove: string) => {
        setTags(tags.filter(tag => tag !== tagToRemove));
    };

    const handleSave = async () => {
        setIsSaving(true);
        try {
            await fileApi.setTags(fileId, tags);
            toast.success('Tags updated successfully');
            onTagsUpdated(tags);
            onClose();
        } catch (error) {
            toast.error('Failed to update tags');
            console.error(error);
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 backdrop-blur-sm" onClick={onClose}>
            <div
                className="bg-[var(--surface-primary)] p-6 rounded-xl shadow-xl w-full max-w-md border border-[var(--border-color)]"
                onClick={e => e.stopPropagation()}
            >
                <div className="flex justify-between items-center mb-4">
                    <h3 className="text-xl font-semibold text-[var(--text-primary)]">Manage Tags</h3>
                    <button onClick={onClose} className="text-[var(--text-secondary)] hover:text-[var(--text-primary)] transition-colors">
                        <X size={20} />
                    </button>
                </div>

                <div className="mb-4 flex flex-wrap gap-2 min-h-[40px] p-2 bg-[var(--surface-secondary)] rounded-lg border border-[var(--border-color)]">
                    {tags.length === 0 && <span className="text-[var(--text-tertiary)] text-sm self-center">No tags yet</span>}
                    {tags.map(tag => (
                        <span key={tag} className="inline-flex items-center px-2.5 py-1 rounded-full text-sm bg-[var(--accent-primary)]/10 text-[var(--accent-primary)] border border-[var(--accent-primary)]/20">
                            {tag}
                            <button
                                onClick={() => handleRemoveTag(tag)}
                                className="ml-1.5 hover:text-red-500 transition-colors"
                            >
                                <X size={14} />
                            </button>
                        </span>
                    ))}
                </div>

                <form onSubmit={handleAddTag} className="flex gap-2 mb-6">
                    <input
                        type="text"
                        value={newTag}
                        onChange={e => setNewTag(e.target.value)}
                        placeholder="Add a tag..."
                        className="flex-1 px-3 py-2 rounded-lg bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-primary)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-primary)]"
                    />
                    <button
                        type="submit"
                        disabled={!newTag.trim()}
                        className="p-2 rounded-lg bg-[var(--surface-secondary)] text-[var(--text-primary)] hover:bg-[var(--surface-primary)] border border-[var(--border-color)] disabled:opacity-50 transition-colors"
                    >
                        <Plus size={20} />
                    </button>
                </form>

                <div className="flex justify-end gap-3">
                    <button
                        onClick={onClose}
                        className="px-4 py-2 rounded-lg text-[var(--text-secondary)] hover:bg-[var(--surface-secondary)] transition-colors"
                    >
                        Cancel
                    </button>
                    <button
                        onClick={handleSave}
                        disabled={isSaving}
                        className="px-4 py-2 rounded-lg bg-[var(--accent-primary)] text-white hover:bg-[var(--accent-secondary)] flex items-center gap-2 transition-colors disabled:opacity-70"
                    >
                        {isSaving ? <Loader2 className="animate-spin" size={18} /> : <Save size={18} />}
                        Save Changes
                    </button>
                </div>
            </div>
        </div>
    );
}
