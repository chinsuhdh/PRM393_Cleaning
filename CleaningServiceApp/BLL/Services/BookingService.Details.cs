using Cleaning.BLL.Common;
using Cleaning.BLL.DTOs;
using Cleaning.DAL.Entities;
using Cleaning.DAL.Enums;

namespace Cleaning.BLL.Services;

public partial class BookingService
{
    public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null) return null;

        await HydrateAsync([booking]);

        var dto = _mapper.Map<BookingDto>(booking);
        await AddDetailCollectionsAsync(booking, dto);
        return dto;
    }

    public async Task<BookingDto?> GetBookingByIdAsync(Guid bookingId, Guid accountId)
    {
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null) return null;
        var isParticipant = booking.ClientId == accountId || booking.WorkerId == accountId;
        if (!isParticipant && !await IsEligibleAsync(booking, accountId))
            throw new AppException(AppErrors.Forbidden);
        await HydrateAsync([booking]);
        var dto = _mapper.Map<BookingDto>(booking);
        await AddDetailCollectionsAsync(booking, dto);
        return dto;
    }

    public async Task<IReadOnlyList<BookingPhotoDto>?> AddPhotosAsync(
        Guid bookingId,
        Guid accountId,
        IReadOnlyList<string> photoUrls)
    {
        var booking = await _unitOfWork.Repository<Booking>().GetByIdAsync(bookingId);
        if (booking == null || booking.ClientId != accountId || photoUrls.Count == 0) return null;
        var existing = (await _unitOfWork.Repository<BookingPhoto>()
            .FindAsync(photo => photo.BookingId == bookingId && photo.PhotoType == PhotoType.Before)).ToList();
        if (existing.Count + photoUrls.Count > 5) return null;
        var created = photoUrls.Select(url => new BookingPhoto
        {
            BookingId = bookingId,
            UploadedBy = accountId,
            PhotoUrl = url,
            PhotoType = PhotoType.Before,
            CreatedAt = DateTime.UtcNow
        }).ToList();
        _unitOfWork.Repository<BookingPhoto>().AddRange(created);
        await _unitOfWork.SaveChangesAsync();
        return created.Select(photo => new BookingPhotoDto
        {
            Id = photo.Id,
            PhotoUrl = photo.PhotoUrl,
            PhotoType = photo.PhotoType.ToString()
        }).ToList();
    }

    private async Task AddDetailCollectionsAsync(Booking booking, BookingDto dto)
    {
        var photos = await _unitOfWork.Repository<BookingPhoto>().FindAsync(photo => photo.BookingId == booking.Id);
        dto.Photos = photos.Select(photo => new BookingPhotoDto
        {
            Id = photo.Id,
            PhotoUrl = photo.PhotoUrl,
            PhotoType = photo.PhotoType.ToString()
        }).ToList();
        var logs = await _unitOfWork.Repository<BookingStatusLog>().FindAsync(log => log.BookingId == booking.Id);
        dto.StatusTimeline = logs.OrderBy(log => log.CreatedAt).Select(log => new BookingStatusLogDto
        {
            OldStatus = log.OldStatus?.ToString(),
            NewStatus = log.NewStatus.ToString(),
            ChangedBy = log.ChangedBy,
            Reason = log.Reason,
            CreatedAt = log.CreatedAt
        }).ToList();
    }
}
