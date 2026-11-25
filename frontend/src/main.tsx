import { createRoot } from 'react-dom/client';
import App from './App';
import './index.css';
import './account.css';
import './search.css';
import './nhatot.css';
import './modern.css';

// Không bật StrictMode để tránh gọi API chi tiết nhà 2 lần (làm tăng lượt xem) ở môi trường dev
createRoot(document.getElementById('root')!).render(<App />);
