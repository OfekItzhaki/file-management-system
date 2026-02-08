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
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 backdrop-blur-sm" onClick={onClose}>
            <div
                className="bg-[var(--surface-primary)] p-8 rounded-2xl shadow-2xl w-full max-w-lg border border-[var(--border-color)]"
                onClick={e => e.stopPropagation()}
            >
                <div className="flex justify-between items-center mb-6">
                    <h3 className="text-2xl font-bold text-[var(--text-primary)]">Manage Tags</h3>
                    <button 
                        onClick={onClose} 
                        className="text-[var(--text-secondary)] hover:text-[var(--text-primary)] hover:bg-[var(--surface-secondary)] p-2 rounded-lg transition-all"
                    >
                        <X size={22} />
                    </button>
                </div>

                <div className="mb-6 flex flex-wrap gap-2.5 min-h-[60px] p-4 bg-[var(--surface-secondary)] rounded-xl border border-[var(--border-color)]">
                    {tags.length === 0 && <span className="text-[var(--text-tertiary)] text-sm self-center">No tags yet</span>}
                    {tags.map(tag => (
                        <span 
                            key={tag} 
                            className="inline-flex items-center px-3.5 py-2 rounded-lg text-sm font-medium bg-gradient-to-r from-[var(--accent-primary)]/15 to-[var(--accent-primary)]/10 text-[var(--accent-primary)] border border-[var(--accent-primary)]/30 hover:border-[var(--accent-primary)]/50 transition-all"
                        >
                            {tag}
                            <button
                                onClick={() => handleRemoveTag(tag)}
                                className="ml-2 hover:text-red-500 hover:scale-110 transition-all"
                            >
                                <X size={16} />
                            </button>
                        </span>
                    ))}
                </div>

                <form onSubmit={handleAddTag} className="flex gap-3 mb-8">
                    <input
                        type="text"
                        value={newTag}
                        onChange={e => setNewTag(e.target.value)}
                        placeholder="Add a tag..."
                        className="flex-1 px-4 py-2.5 rounded-lg bg-[var(--bg-primary)] border border-[var(--border-color)] text-[var(--text-primary)] placeholder:text-[var(--text-tertiary)] focus:outline-none focus:ring-2 focus:ring-[var(--accent-primary)] focus:border-transparent transition-all"
                    />
                    <button
                        type="submit"
                        disabled={!newTag.trim()}
                        className="p-2.5 rounded-lg bg-[var(--accent-primary)]/10 text-[var(--accent-primary)] hover:bg-[var(--accent-primary)]/20 border border-[var(--accent-primary)]/30 disabled:opacity-40 disabled:cursor-not-allowed transition-all"
                    >
                        <Plus size={22} />
                    </button>
                </form>

                <div className="flex justify-end gap-3">
                    <button
                        onClick={onClose}
                        className="px-5 py-2.5 rounded-lg text-[var(--text-secondary)] hover:bg-[var(--surface-secondary)] font-medium transition-all"
                    >
                        Cancel
                    </button>
                    <button
                        onClick={handleSave}
                        disabled={isSaving}
                        className="px-5 py-2.5 rounded-lg bg-[var(--accent-primary)] text-white hover:bg-[var(--accent-secondary)] flex items-center gap-2 font-medium transition-all disabled:opacity-70 shadow-lg shadow-[var(--accent-primary)]/20"
                    >
                        {isSaving ? <Loader2 className="animate-spin" size={18} /> : <Save size={18} />}
                        Save Changes
                    </button>
                </div>
            </div>
        </div>
    );
}
