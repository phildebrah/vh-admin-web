﻿using AdminWebsite.BookingsAPI.Client;
using FluentValidation;

namespace AdminWebsite.Validators
{
    public class ParticipantRequestValidation : AbstractValidator<ParticipantRequest>
    {
        public static readonly string ContactEmailMessage = "Email is required in the correct format and between 1 - 255 characters";
        public static readonly string DisplayNameMessage = "Display name is required and between 1 - 255 characters";
        public static readonly string FirstNameMessage = "First name is required and between 1 - 255 characters";
        public static readonly string LastNameMessage = "Lastname is required and between 1 - 255 characters";

        public ParticipantRequestValidation()
        {
            RuleFor(x => x.Contact_email).NotEmpty().EmailAddress().MaximumLength(255).WithMessage(ContactEmailMessage);
            RuleFor(x => x.Display_name).NotEmpty().MaximumLength(255).WithMessage(DisplayNameMessage);
            RuleFor(x => x.First_name).NotEmpty().MaximumLength(255).WithMessage(FirstNameMessage);
            RuleFor(x => x.Last_name).NotEmpty().MaximumLength(255).WithMessage(LastNameMessage);
        }
    }
}